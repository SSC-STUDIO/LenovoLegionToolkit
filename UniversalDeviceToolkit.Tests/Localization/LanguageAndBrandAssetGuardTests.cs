using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Localization;

/// <summary>
/// Language-gate ordering, online footprint, and brand asset invariants.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public sealed class LanguageAndBrandAssetGuardTests
{
    [Fact]
    public void StartupOrchestrator_LanguageGate_RunsBeforeMainWindowCreate()
    {
        var root = FindRoot();
        var path = Path.Combine(root, "UniversalDeviceToolkit.WPF", "Startup", "StartupOrchestrator.cs");
        var text = File.ReadAllText(path);

        var gate = text.IndexOf("RunLanguageGateAsync()", StringComparison.Ordinal);
        var create = text.IndexOf("CreateMainWindowAsync()", StringComparison.Ordinal);
        var show = text.IndexOf("ShowMainWindowAsync()", StringComparison.Ordinal);

        gate.Should().BeGreaterThan(0, "language gate must be invoked");
        create.Should().BeGreaterThan(gate, "MainWindow must not be created before language gate");
        show.Should().BeGreaterThan(create, "MainWindow show must follow create");
    }

    [Fact]
    public void StartupOrchestrator_LanguageGateExit_DoesNotCreateMainWindow()
    {
        var root = FindRoot();
        var path = Path.Combine(root, "UniversalDeviceToolkit.WPF", "Startup", "StartupOrchestrator.cs");
        var text = File.ReadAllText(path);

        text.Should().Contain("Language gate exited; shutting down without creating MainWindow");
        text.Should().Contain("LanguageGateOutcome.Exit");
    }

    [Fact]
    public void OnlineShipping_PruneScript_ExistsAndAcceptsAllowedCultures()
    {
        var root = FindRoot();
        var prune = Path.Combine(root, "Scripts", "Prune-ShippingFootprint.ps1");
        File.Exists(prune).Should().BeTrue();
        var text = File.ReadAllText(prune);
        text.Should().Contain("AllowedCultures");
        text.Should().Contain("Pruned satellite culture");
    }

    [Fact]
    public void BrandIcon_Paths_AreConsistentAcrossPackageSiteAndApp()
    {
        var root = FindRoot();

        // Canonical Trace brand set (no redesign this round).
        var requiredFiles = new[]
        {
            "Assets/Logo.png",
            "Assets/Icon.ico",
            "Assets/Logo.png",
            "Assets/Screenshot_main.png",
            "Assets/Brand/udt-symbol.svg",
            "Assets/Brand/udt-symbol-dark.svg",
            "Assets/Brand/udt-symbol-light.svg",
            "Assets/Brand/tray-dark.png",
            "Assets/Brand/tray-light.png",
            "Assets/Brand/icon-256.png",
            "site/index.html"
        };

        foreach (var relative in requiredFiles)
        {
            var full = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(full).Should().BeTrue($"missing brand asset: {relative}");
        }

        // Site OG uses the canonical product screenshot.
        var site = File.ReadAllText(Path.Combine(root, "site", "index.html"));
        site.Should().Contain("Screenshot_main.png");

        // README uses Assets/Logo.png.
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        readme.Should().Contain("Assets/Logo.png");

        // Installer uninstall icon uses the app executable (embeds Icon.ico).
        var engine = File.ReadAllText(Path.Combine(root, "Tools", "Installer", "InstallerEngine.cs"));
        engine.Should().Contain("DisplayIcon");
        engine.Should().Contain("InstallerConstants.MainExeName");

        // Tray helper uses AssetResources.icon (from Icon.ico).
        var tray = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Utils", "TrayHelper.cs"));
        tray.Should().Contain("AssetResources.icon");
    }

    [Fact]
    public void BrandAssets_Screenshot_AndIconIco_HaveNonTrivialSize()
    {
        var root = FindRoot();
        new FileInfo(Path.Combine(root, "Assets", "Screenshot_main.png")).Length.Should().BeGreaterThan(1024);
        new FileInfo(Path.Combine(root, "Assets", "Icon.ico")).Length.Should().BeGreaterThan(1024);
        new FileInfo(Path.Combine(root, "Assets", "Logo.png")).Length.Should().BeGreaterThan(1024);
    }

    [Fact]
    public void Csproj_ApplicationIcon_PointsAtCanonicalRootAssets()
    {
        var root = FindRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "UniversalDeviceToolkit.WPF.csproj"));
        csproj.Should().Contain(@"..\Assets\Icon.ico");
        csproj.Should().Contain(@"Link=""Assets\Icon.ico""");
        // WPF project must not keep a second physical copy under WPF/Assets for brand binaries.
        File.Exists(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Assets", "Icon.ico")).Should().BeFalse();
        File.Exists(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Assets", "Logo.png")).Should().BeFalse();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
