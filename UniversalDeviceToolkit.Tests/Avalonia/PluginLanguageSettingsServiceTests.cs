#if WINDOWS

using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Tests;
using Xunit;
using CustomMouseResource = UniversalDeviceToolkit.Tests.Avalonia.Resources.Resource;
using ShellResource = UniversalDeviceToolkit.Tests.Avalonia.Plugins.Resources.Resource;

namespace UniversalDeviceToolkit.Tests.Avalonia
{
    [Collection(TestCollections.Localization)]
    [Trait("Category", TestCategories.Unit)]
    public sealed class PluginLanguageSettingsServiceTests : IDisposable
    {
        private readonly string _settingsFilePath;
        private readonly string _appDataDirectory;
        private readonly List<PluginLanguageService> _services = [];
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;
        private readonly CultureInfo? _previousDefaultCulture;
        private readonly CultureInfo? _previousDefaultUiCulture;

        public PluginLanguageSettingsServiceTests()
        {
            _appDataDirectory = Path.Combine(Path.GetTempPath(), $"udt-plugin-language-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_appDataDirectory);
            _settingsFilePath = Path.Combine(_appDataDirectory, "plugin-languages.json");
            _previousCulture = CultureInfo.CurrentCulture;
            _previousUiCulture = CultureInfo.CurrentUICulture;
            _previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
            _previousDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        }

        public void Dispose()
        {
            foreach (var service in _services)
                service.UnsubscribeLocalization();
            _services.Clear();

            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = _previousDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _previousDefaultUiCulture;

            try
            {
                if (Directory.Exists(_appDataDirectory))
                    Directory.Delete(_appDataDirectory, recursive: true);
            }
            catch
            {
                // Test cleanup must not hide the assertion that already ran.
            }
        }

        [Fact]
        public void GetLanguage_WithoutOverride_ReturnsNull()
        {
            var service = CreateService();

            service.GetLanguage("custom-mouse").Should().BeNull();
        }

        [Fact]
        public void SetLanguage_GetLanguage_RoundTripsThroughPersistentStore()
        {
            var service = CreateService();
            service.SetLanguage("custom-mouse", "zh-Hans");

            var reloaded = CreateService();
            reloaded.GetLanguage("custom-mouse").Should().Be("zh-Hans");
        }

        [Fact]
        public void SetLanguage_Null_FollowsAppLanguageAndClearsStore()
        {
            var service = CreateService();
            service.SetLanguage("custom-mouse", "de");
            service.SetLanguage("custom-mouse", null);

            service.GetLanguage("custom-mouse").Should().BeNull();
            var reloaded = CreateService();
            reloaded.GetLanguage("custom-mouse").Should().BeNull();
        }

        [Fact]
        public void SetLanguage_RaisesLanguagesChanged()
        {
            var service = CreateService();
            var raised = 0;
            service.LanguagesChanged += () => raised++;

            service.SetLanguage("custom-mouse", "de");
            service.SetLanguage("custom-mouse", null);

            raised.Should().Be(2);
        }

        [Fact]
        public void SetLanguage_AppliesOverrideImmediately()
        {
            var service = CreateService();
            CustomMouseResource.Culture = null;

            service.SetLanguage("custom-mouse", "zh-Hans");

            CustomMouseResource.Culture!.Name.Should().Be("zh-Hans");
        }

        [Fact]
        public async Task CultureChange_ReappliesOverridesAndAppCulture()
        {
            var service = CreateService();
            service.SetLanguage("custom-mouse", "zh-Hans");
            service.SetLanguage("shell-integration", null);
            CustomMouseResource.Culture = null;
            ShellResource.Culture = null;

            var targetCulture = new CultureInfo("de");
            if (targetCulture.Name.Equals(_previousUiCulture.Name, StringComparison.OrdinalIgnoreCase))
                targetCulture = new CultureInfo("fr");
            await LocalizationRuntime.SetCultureAsync(targetCulture, persist: false);

            CustomMouseResource.Culture!.Name.Should().Be("zh-Hans");
            ShellResource.Culture!.Name.Should().Be(targetCulture.Name);
        }

        [Fact]
        public void ApplyForAllLoadedPlugins_RestoresOverridesAfterAHostCultureRefresh()
        {
            var service = CreateService();
            service.SetLanguage("custom-mouse", "zh-Hans");
            service.SetLanguage("shell-integration", null);

            // This is the state the old App host path produced: a direct
            // resource refresh applies the app language to every plugin.
            AvaloniaPluginResourceCulture.Apply(
                new CultureInfo("de"),
                new Dictionary<string, string>(),
                TestPluginResourceTypeProvider());
            CustomMouseResource.Culture!.Name.Should().Be("de");

            service.ApplyForAllLoadedPlugins();

            CustomMouseResource.Culture!.Name.Should().Be("zh-Hans");
            ShellResource.Culture!.Name.Should().Be(LocalizationRuntime.CurrentCulture.Name);
        }

        [Fact]
        public void AppLanguageRefresh_DelegatesPluginResourcesToOverrideAwareService()
        {
            var root = RepositoryPaths.FindRoot();
            var app = File.ReadAllText(Path.Combine(
                root,
                "UniversalDeviceToolkit.Avalonia",
                "App.axaml.cs"));

            app.Should().Contain("PluginLanguageService.Current.ApplyForAllLoadedPlugins();");
            app.Should().NotContain("AvaloniaPluginResourceCulture.Apply(e.Culture);");
            app.Should().NotContain("AvaloniaPluginResourceCulture.Apply(LocalizationRuntime.CurrentCulture);");
        }

        [Fact]
        public void Apply_WithPerPluginOverrides_UsesOverrideOrAppCulture()
        {
            var overrides = new Dictionary<string, string>
            {
                ["custom-mouse"] = "zh-Hans",
                ["shell-integration"] = string.Empty,
            };
            var resourceTypes = new Dictionary<string, IEnumerable<Type>>
            {
                ["custom-mouse"] = [typeof(CustomMouseResource)],
                ["shell-integration"] = [typeof(ShellResource)],
            };
            CustomMouseResource.Culture = null;
            ShellResource.Culture = null;

            AvaloniaPluginResourceCulture.Apply(new CultureInfo("fr"), overrides, resourceTypes);

            CustomMouseResource.Culture!.Name.Should().Be("zh-Hans");
            ShellResource.Culture!.Name.Should().Be("fr");
        }

        [Fact]
        public void ResolvePluginCulture_InvalidOverride_FallsBackToAppCulture()
        {
            var overrides = new Dictionary<string, string>
            {
                ["custom-mouse"] = "not-a-real-culture",
            };

            AvaloniaPluginResourceCulture.ResolvePluginCulture(
                    new CultureInfo("de"),
                    overrides,
                    "custom-mouse")
                .Name.Should().Be("de");
        }

        private PluginLanguageService CreateService() =>
            CreateService(TestPluginResourceTypeProvider);

        private PluginLanguageService CreateService(Func<IReadOnlyDictionary<string, IEnumerable<Type>>>? provider)
        {
            var service = new PluginLanguageService(new PluginLanguageSettings(_settingsFilePath), provider);
            _services.Add(service);
            return service;
        }

        private static IReadOnlyDictionary<string, IEnumerable<Type>> TestPluginResourceTypeProvider() =>
            new Dictionary<string, IEnumerable<Type>>
            {
                ["custom-mouse"] = [typeof(CustomMouseResource)],
                ["shell-integration"] = [typeof(ShellResource)],
            };
    }
}

namespace UniversalDeviceToolkit.Tests.Avalonia.Resources
{
    public sealed class Resource
    {
        public static CultureInfo? Culture { get; set; }

        public static ResourceManager ResourceManager { get; } = new(
            "UniversalDeviceToolkit.Tests.Avalonia.Resources.Resource",
            typeof(Resource).Assembly);
    }
}

namespace UniversalDeviceToolkit.Tests.Avalonia.Plugins.Resources
{
    public sealed class Resource
    {
        public static CultureInfo? Culture { get; set; }

        public static ResourceManager ResourceManager { get; } = new(
            "UniversalDeviceToolkit.Tests.Avalonia.Plugins.Resources.Resource",
            typeof(Resource).Assembly);
    }
}

#endif
