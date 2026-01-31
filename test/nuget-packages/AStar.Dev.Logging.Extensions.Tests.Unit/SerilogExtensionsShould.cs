using Serilog;
using ILogger = Serilog.ILogger;

namespace AStar.Dev.Logging.Extensions.Serilog.Tests.Unit;

public class SerilogExtensionsShould
{
    [Fact]
    public void CreateMinimalLoggerWhichWritesToConsoleAndHonoursTheMinimumLogLevelOfDebug()
    {
        ILogger logger = SerilogExtensions.CreateMinimalLogger();
        var verboseToken = $"VERBOSE-{Guid.CreateVersion7():N0}";
        var debugToken = $"DEBUG-{Guid.CreateVersion7():N0}";
        var infoToken = $"INFO-{Guid.CreateVersion7():N0}";

        TextWriter originalOut = Console.Out;
        using var capture = new StringWriter();

        try
        {
            Console.SetOut(capture);

            logger.Verbose("{Token}", verboseToken);
            logger.Debug("{Token}", debugToken);
            logger.Information("{Token}", infoToken);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = capture.ToString();

        _ = output.ShouldNotBeNull();
        output.ShouldContain(debugToken);
        output.ShouldContain(infoToken);
        output.ShouldNotContain(verboseToken);
    }

    [Fact]
    public void ConfigureAStarDevelopmentLoggingDefaultsCanConfigureWithoutFileSink()
    {
        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var loggerConfig = new LoggerConfiguration();
        _ = loggerConfig.ConfigureAStarDevelopmentLoggingDefaults(cfg, false);
        ILogger logger = loggerConfig.CreateLogger();

        var token = $"NOFILE-{Guid.CreateVersion7():N0}";
        Should.NotThrow(() => logger.Information("{Token}", token));
    }

    [Fact]
    public void ConfigureAStarDevelopmentLoggingDefaultsOverridesMicrosoftToInformation()
    {
        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var loggerConfig = new LoggerConfiguration();
        _ = loggerConfig.ConfigureAStarDevelopmentLoggingDefaults(cfg, false);
        ILogger logger = loggerConfig.CreateLogger();

        var infoToken = $"MS-I-{Guid.CreateVersion7():N0}";
        ILogger microsoftLogger = logger.ForContext("SourceContext", "Microsoft");
        Should.NotThrow(() => microsoftLogger.Information("{Token}", infoToken));
    }
}
