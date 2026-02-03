using Microsoft.Extensions.Configuration;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;

internal sealed class ConfigurationMarker
{
}

public static class ConfigurationFactory
{
    public static IConfiguration Build(string[] args, string? basePath = null, string? environment = null)
    {
        var resolvedEnvironment = environment
                                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                                ?? "Production";

        var resolvedBasePath = basePath ?? Directory.GetCurrentDirectory();

        return new ConfigurationBuilder()
            .SetBasePath(resolvedBasePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{resolvedEnvironment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<ConfigurationMarker>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }
}
