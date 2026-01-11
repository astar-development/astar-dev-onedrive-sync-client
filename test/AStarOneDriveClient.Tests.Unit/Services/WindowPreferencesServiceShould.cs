using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Services;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Tests.Unit.Services;

public class WindowPreferencesServiceShould
{
    [Fact]
    public async Task ReturnNullWhenNoPreferencesExist()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);

        var result = await service.LoadAsync(CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveNewPreferencesWhenNoneExist()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);
        var preferences = new WindowPreferences(0, 100, 200, 1024, 768, false);

        await service.SaveAsync(preferences, CancellationToken.None);

        var saved = await context.WindowPreferences.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        saved.ShouldNotBeNull();
        saved.X.ShouldBe(100);
        saved.Y.ShouldBe(200);
        saved.Width.ShouldBe(1024);
        saved.Height.ShouldBe(768);
        saved.IsMaximized.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateExistingPreferencesWhenTheyExist()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);
        var initialPrefs = new WindowPreferences(0, 100, 200, 800, 600, false);
        await service.SaveAsync(initialPrefs, CancellationToken.None);

        var updatedPrefs = new WindowPreferences(1, 150, 250, 1280, 720, true);
        await service.SaveAsync(updatedPrefs, CancellationToken.None);

        var allPreferences = await context.WindowPreferences.ToListAsync(TestContext.Current.CancellationToken);
        allPreferences.Count.ShouldBe(1);

        var saved = allPreferences[0];
        saved.X.ShouldBe(150);
        saved.Y.ShouldBe(250);
        saved.Width.ShouldBe(1280);
        saved.Height.ShouldBe(720);
        saved.IsMaximized.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadSavedPreferencesCorrectly()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);
        var preferences = new WindowPreferences(0, 300, 400, 1920, 1080, false);
        await service.SaveAsync(preferences, CancellationToken.None);

        var loaded = await service.LoadAsync(CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.X.ShouldBe(300);
        loaded.Y.ShouldBe(400);
        loaded.Width.ShouldBe(1920);
        loaded.Height.ShouldBe(1080);
        loaded.IsMaximized.ShouldBeFalse();
    }

    [Fact]
    public async Task SavePreferencesWithNullPositionWhenMaximized()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);
        var preferences = new WindowPreferences(0, null, null, 1024, 768, true);

        await service.SaveAsync(preferences, CancellationToken.None);

        var loaded = await service.LoadAsync(CancellationToken.None);
        loaded.ShouldNotBeNull();
        loaded.X.ShouldBeNull();
        loaded.Y.ShouldBeNull();
        loaded.Width.ShouldBe(1024);
        loaded.Height.ShouldBe(768);
        loaded.IsMaximized.ShouldBeTrue();
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenSavingNullPreferences()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);

        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await service.SaveAsync(null!, CancellationToken.None)
        );

        exception.ParamName.ShouldBe("preferences");
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenContextIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(
            () => new WindowPreferencesService(null!)
        );

        exception.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task RespectCancellationTokenWhenLoading()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await service.LoadAsync(cts.Token)
        );
    }

    [Fact]
    public async Task RespectCancellationTokenWhenSaving()
    {
        using var context = CreateInMemoryContext();
        var service = new WindowPreferencesService(context);
        var preferences = new WindowPreferences(0, 100, 200, 800, 600, false);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await service.SaveAsync(preferences, cts.Token)
        );
    }

    private static SyncDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new SyncDbContext(options);
    }
}
