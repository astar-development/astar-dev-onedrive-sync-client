using System.Diagnostics;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// Linux-specific secure token storage using Secret Service API (D-Bus).
/// Uses the 'secret-tool' command-line utility to interact with the system keyring.
/// </summary>
public class LinuxSecureTokenStorage : ISecureTokenStorage
{
    private const string ApplicationName = "AStar.OneDriveSync";

    /// <inheritdoc/>
    public string Name => "Linux Secret Service";

    /// <inheritdoc/>
    public bool IsAvailable => OperatingSystem.IsLinux() && IsSecretToolAvailable();

    /// <inheritdoc/>
    public async Task StoreTokenAsync(string key, string token)
    {
        if(!IsAvailable)
            throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux.");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = $"store --label=\"{key}\" application \"{ApplicationName}\" key \"{key}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _ = process.Start();

        // Write the token to stdin
        await process.StandardInput.WriteAsync(token);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();

        await process.WaitForExitAsync();

        if(process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to store token in Secret Service: {error}");
        }
    }

    /// <inheritdoc/>
    public async Task<string?> RetrieveTokenAsync(string key)
    {
        if(!IsAvailable)
            throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux.");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = $"lookup application \"{ApplicationName}\" key \"{key}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _ = process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return process.ExitCode != 0 ? null : output.TrimEnd('\n', '\r');
    }

    /// <inheritdoc/>
    public async Task DeleteTokenAsync(string key)
    {
        if(!IsAvailable)
            throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux.");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = $"clear application \"{ApplicationName}\" key \"{key}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _ = process.Start();
        await process.WaitForExitAsync();
    }

    private static bool IsSecretToolAvailable()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "secret-tool",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            _ = process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
