using System.Diagnostics;
using System.Text;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// macOS-specific secure token storage using Keychain.
/// Uses the 'security' command-line tool to interact with the system Keychain.
/// </summary>
public class MacOSSecureTokenStorage : ISecureTokenStorage
{
    private const string ServiceName = "AStar.OneDriveSync";
    private const string AccountName = "TokenStorage";

    /// <inheritdoc/>
    public string Name => "macOS Keychain";

    /// <inheritdoc/>
    public bool IsAvailable => OperatingSystem.IsMacOS() && IsSecurityCommandAvailable();

    /// <inheritdoc/>
    public async Task StoreTokenAsync(string key, string token)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS.");

        // Delete existing token if present (security command will fail if it already exists)
        await DeleteTokenAsync(key);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"add-generic-password -a \"{AccountName}\" -s \"{GetServiceKey(key)}\" -w \"{token}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to store token in Keychain: {error}");
        }
    }

    /// <inheritdoc/>
    public async Task<string?> RetrieveTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS.");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"find-generic-password -a \"{AccountName}\" -s \"{GetServiceKey(key)}\" -w",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            // Token not found or other error
            return null;
        }

        return output.TrimEnd('\n', '\r');
    }

    /// <inheritdoc/>
    public async Task DeleteTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS.");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"delete-generic-password -a \"{AccountName}\" -s \"{GetServiceKey(key)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        // Exit code 44 means the item was not found, which is acceptable
        // We don't throw an error if the token doesn't exist
    }

    private static string GetServiceKey(string key)
    {
        return $"{ServiceName}.{key}";
    }

    private static bool IsSecurityCommandAvailable()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "security",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}