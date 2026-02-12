using System.IO;

namespace AStar.Dev.OneDrive.Client.Tests.Integration;

public class ThemeResourceDictionariesShould
{
    [Fact]
    public void ProfessionalThemeFile_ShouldExist()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var themeFilePath = Path.Combine(projectRoot, "src", "AStar.Dev.OneDrive.Client", "Themes", "ProfessionalTheme.axaml");

        // Act & Assert
        File.Exists(themeFilePath).ShouldBeTrue($"Professional theme file should exist at {themeFilePath}");
    }

    [Fact]
    public void ColourfulThemeFile_ShouldExist()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var themeFilePath = Path.Combine(projectRoot, "src", "AStar.Dev.OneDrive.Client", "Themes", "ColourfulTheme.axaml");

        // Act & Assert
        File.Exists(themeFilePath).ShouldBeTrue($"Colourful theme file should exist at {themeFilePath}");
    }

    [Fact]
    public void TerminalThemeFile_ShouldExist()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var themeFilePath = Path.Combine(projectRoot, "src", "AStar.Dev.OneDrive.Client", "Themes", "TerminalTheme.axaml");

        // Act & Assert
        File.Exists(themeFilePath).ShouldBeTrue($"Terminal theme file should exist at {themeFilePath}");
    }

    [Fact]
    public void ThemeFiles_ShouldHaveValidXamlStructure()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var themesFolder = Path.Combine(projectRoot, "src", "AStar.Dev.OneDrive.Client", "Themes");
        var themeFiles = new[] { "ProfessionalTheme.axaml", "ColourfulTheme.axaml", "TerminalTheme.axaml" };

        // Act & Assert
        foreach (var themeFile in themeFiles)
        {
            var filePath = Path.Combine(themesFolder, themeFile);
            var content = File.ReadAllText(filePath);
            
            // Verify it's a Styles file
            content.ShouldContain("<Styles");
            content.ShouldContain("https://github.com/avaloniaui");
        }
    }

    private string GetProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "AStar.Dev.OneDrive.Sync.Client.slnx")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return currentDir ?? throw new InvalidOperationException("Could not find project root");
    }
}
