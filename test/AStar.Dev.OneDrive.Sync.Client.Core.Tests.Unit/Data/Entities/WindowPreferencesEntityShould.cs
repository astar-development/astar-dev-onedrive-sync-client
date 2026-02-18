using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.ConfigurationSettings;

// add tests for WindowPreferencesEntity
public class WindowPreferencesEntityShould
{
    [Fact]
    public void CreateWindowPreferencesEntityWithRequiredProperties()
    {
        var preferencesEntity = new WindowPreferencesEntity
        {
            Id = 1,
            IsMaximized = true,
            Width = 800,
            Height = 600,
            X = 100,
            Y = 100,
            Theme = "Dark"
        };

        preferencesEntity.Id.ShouldBe(1);
        preferencesEntity.IsMaximized.ShouldBeTrue();
        preferencesEntity.Width.ShouldBe(800);
        preferencesEntity.Height.ShouldBe(600);
        preferencesEntity.X.ShouldBe(100);
        preferencesEntity.Y.ShouldBe(100);
        preferencesEntity.Theme.ShouldBe("Dark");
    }
}
