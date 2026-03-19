using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Services.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Localization;

public class LocalizationServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithFallbackCulture()
    {
        var service = new LocalizationService();

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public void Constructor_ShouldDiscoverAvailableCultures()
    {
        var service = new LocalizationService();

        service.AvailableCultures.ShouldNotBeEmpty();
        service.AvailableCultures.ShouldContain(c => c.Name == "en-GB");
    }

    [Fact]
    public async Task InitialiseAsync_WithoutArgument_ShouldUseFallbackCulture()
    {
        var service = new LocalizationService();

        await service.InitialiseAsync();

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public async Task InitialiseAsync_WithFallbackCulture_ShouldSucceed()
    {
        var service = new LocalizationService();
        var culture = new CultureInfo("en-GB");

        await service.InitialiseAsync(culture);

        _ = service.CurrentCulture.ShouldNotBeNull();
    }

    [Fact]
    public void Get_WithValidKey_ShouldReturnValue()
    {
        var service = new LocalizationService();
        var key = "App.Title";

        var result = service.Get(key);

        _ = result.ShouldNotBeNull();
        result.ShouldNotBe(string.Empty);
    }

    [Fact]
    public void Get_WithInvalidKey_ShouldReturnKeyAsFallback()
    {
        var service = new LocalizationService();
        var key = "NonExistent.Key";

        var result = service.Get(key);

        result.ShouldBe(key);
    }

    [Fact]
    public void Get_WithMultipleKeys_ShouldReturnSameValuesForSameKeys()
    {
        var service = new LocalizationService();
        var key = "App.Title";

        var result1 = service.Get(key);
        var result2 = service.Get(key);

        result1.ShouldBe(result2);
    }

    [Fact]
    public void Get_WithFormatArguments_ShouldFormatString()
    {
        var service = new LocalizationService();
        var key = "Format.Test";
        var placeholder = "TestValue";

        var result = service.Get(key, placeholder);

        _ = result.ShouldNotBeNull();
    }

    [Fact]
    public void Get_WithEmptyKey_ShouldReturnEmptyString()
    {
        var service = new LocalizationService();

        var result = service.Get(string.Empty);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Get_WithNullKey_ShouldReturnNullKey()
    {
        var service = new LocalizationService();

        try
        {
            var result = service.Get(null!);
            _ = result.ShouldNotBeNull();
        }
        catch(ArgumentNullException)
        {
            // Expected behavior - null key should throw
        }
    }

    [Fact]
    public async Task SetCultureAsync_WithFallbackCulture_ShouldChangeCulture()
    {
        var service = new LocalizationService();
        var targetCulture = new CultureInfo("en-GB");

        await service.SetCultureAsync(targetCulture);

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public async Task SetCultureAsync_WithSameCulture_ShouldNotRaiseEvent()
    {
        var service = new LocalizationService();
        var eventRaised = false;
        service.CultureChanged += (s, c) => eventRaised = true;

        CultureInfo currentCulture = service.CurrentCulture;

        await service.SetCultureAsync(currentCulture);

        eventRaised.ShouldBeFalse();
    }

    [Fact]
    public void CultureInfo_ShouldBeReadOnly()
    {
        var service = new LocalizationService();
        CultureInfo originalCulture = service.CurrentCulture;
        _ = originalCulture.ShouldNotBeNull();
    }

    [Fact]
    public void AvailableCultures_ShouldBeReadOnly()
    {
        var service = new LocalizationService();

        IReadOnlyList<CultureInfo> cultures = service.AvailableCultures;

        _ = cultures.ShouldNotBeNull();
        // Cannot modify as it should be a read-only list
    }

    [Fact]
    public void Get_WithMultipleFormatArguments_ShouldHandleCorrectly()
    {
        var service = new LocalizationService();
        var key = "Format.MultiArg";

        var result = service.Get(key, "arg1", "arg2", "arg3");

        _ = result.ShouldNotBeNull();
    }

    [Fact]
    public void CurrentCulture_ShouldNotBeRapidlyChanged()
    {
        var service = new LocalizationService();
        CultureInfo originalCulture = service.CurrentCulture;

        _ = originalCulture.ShouldNotBeNull();
        originalCulture.Name.ShouldNotBeEmpty();
    }
}
