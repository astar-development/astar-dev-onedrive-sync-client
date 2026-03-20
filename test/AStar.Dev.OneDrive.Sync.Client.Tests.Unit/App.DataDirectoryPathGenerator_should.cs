namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit;

public class App
{
    // ReSharper disable InconsistentNaming
    public class DataDirectoryPathGenerator_should
    {
        [Fact]
        public void generate_a_valid_directory_for_the_user_email_when_on_Windows()
        {
            if(!OperatingSystem.IsWindows()) return;
            const string expected = "Documents/AStar.Dev.OneDrive.Sync/test-email_example_com";

            var actual = DataDirectoryPathGenerator.GetPlatformUserDataDirectory("test@email.example.com");

            actual.ShouldEndWith(expected);
        }

        [Fact]
        public void generate_a_valid_directory_for_the_user_email_when_on_MacOs()
        {
            if(!OperatingSystem.IsMacOS()) return;
            const string expected = "Documents/AStar.Dev.OneDrive.Sync/test-email_example_com";

            var actual = DataDirectoryPathGenerator.GetPlatformUserDataDirectory("test@email.example.com");

            actual.ShouldEndWith(expected);
        }

        [Fact]
        public void generate_a_valid_directory_for_the_user_email_when_on_Linux()
        {
            if(!OperatingSystem.IsLinux()) return;
            const string expected = "Documents/AStar.Dev.OneDrive.Sync/test-email_example_com";

            var actual = DataDirectoryPathGenerator.GetPlatformUserDataDirectory("test@email.example.com");

            actual.ShouldEndWith(expected);
        }
        [Fact]
        public void generate_a_valid_data_directory_when_on_Windows()
        {
            if(!OperatingSystem.IsWindows()) return;
            const string expected = "AppData/Roaming/AStar.Dev.OneDrive.Sync";

            var actual = DataDirectoryPathGenerator.GetPlatformDataDirectory();

            actual.ShouldEndWith(expected);
        }

        [Fact]
        public void generate_a_valid_data_directory_when_on_MacOs()
        {
            if(!OperatingSystem.IsMacOS()) return;
            const string expected = "Library/Application Support/AStar.Dev.OneDrive.Sync";

            var actual = DataDirectoryPathGenerator.GetPlatformDataDirectory();

            actual.ShouldEndWith(expected);
        }

        [Fact]
        public void generate_a_valid_data_directory_when_on_Linux()
        {
            if(!OperatingSystem.IsLinux()) return;
            const string expected = ".config/AStar.Dev.OneDrive.Sync";

            var actual = DataDirectoryPathGenerator.GetPlatformDataDirectory();

            actual.ShouldEndWith(expected);
        }
    }
}
