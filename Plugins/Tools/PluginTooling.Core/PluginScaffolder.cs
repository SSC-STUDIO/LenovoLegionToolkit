namespace PluginTooling.Core;

public sealed class PluginScaffolder
{
    private const string DefaultIcon = "PuzzlePiece24";

    private readonly PluginRepository _repository = new();
    private readonly ProcessRunner _processRunner = new();

    public async Task<ScaffoldResult> CreateAsync(ScaffoldRequest request, Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        var repository = _repository.Load(request.RepositoryRoot);
        var archetype = _repository.LoadArchetypeDefinition(repository.RootPath, request.Template);

        var pluginDirectory = Path.Combine(repository.PluginsRoot, request.FolderName);
        var testsDirectory = Path.Combine(repository.PluginsRoot, $"{request.FolderName}.Tests");
        if (Directory.Exists(pluginDirectory))
            throw new InvalidOperationException($"Plugin directory already exists: {pluginDirectory}");

        if (Directory.Exists(testsDirectory))
            throw new InvalidOperationException($"Plugin test directory already exists: {testsDirectory}");

        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(testsDirectory);
        Directory.CreateDirectory(Path.Combine(pluginDirectory, "Resources"));

        var namespaceSegment = string.IsNullOrWhiteSpace(request.NamespaceSegment)
            ? PluginRepository.NormalizeIdentifier(request.FolderName)
            : PluginRepository.NormalizeIdentifier(request.NamespaceSegment);

        var classPrefix = string.IsNullOrWhiteSpace(request.ClassPrefix)
            ? PluginRepository.NormalizeIdentifier(request.FolderName)
            : PluginRepository.NormalizeIdentifier(request.ClassPrefix);

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"{request.DisplayName} plugin for Lenovo Legion Toolkit"
            : request.Description.Trim();

        var pluginProjectPath = Path.Combine(pluginDirectory, $"LenovoLegionToolkit.Plugins.{request.FolderName}.csproj");
        var testProjectPath = Path.Combine(testsDirectory, $"{request.FolderName}.Tests.csproj");

        File.WriteAllText(pluginProjectPath, PluginRepository.NormalizeLineEndings(BuildProjectFile(request)));
        File.WriteAllText(testProjectPath, PluginRepository.NormalizeLineEndings(BuildTestProjectFile(request, namespaceSegment)));
        File.WriteAllText(Path.Combine(pluginDirectory, "plugin.json"), PluginRepository.NormalizeLineEndings(BuildPluginJson(request)));
        File.WriteAllText(Path.Combine(pluginDirectory, "CHANGELOG.md"), PluginRepository.NormalizeLineEndings(BuildPluginChangelog(request.DisplayName)));
        File.WriteAllText(Path.Combine(pluginDirectory, $"{classPrefix}Text.cs"), PluginRepository.NormalizeLineEndings(BuildTextClass(namespaceSegment, classPrefix, request)));
        File.WriteAllText(Path.Combine(pluginDirectory, $"{classPrefix}Plugin.cs"), PluginRepository.NormalizeLineEndings(BuildPluginClass(namespaceSegment, classPrefix, request, description, archetype)));
        File.WriteAllText(Path.Combine(pluginDirectory, $"{classPrefix}Control.xaml"), PluginRepository.NormalizeLineEndings(BuildContentControlXaml(classPrefix, request.DisplayName, "Feature preview")));
        File.WriteAllText(Path.Combine(pluginDirectory, $"{classPrefix}Control.xaml.cs"), PluginRepository.NormalizeLineEndings(BuildControlCodeBehind(namespaceSegment, classPrefix, "Control")));

        if (archetype.HasSettingsPage)
        {
            File.WriteAllText(Path.Combine(pluginDirectory, $"{classPrefix}SettingsControl.xaml"), PluginRepository.NormalizeLineEndings(BuildContentControlXaml($"{classPrefix}Settings", $"{request.DisplayName} Settings", "Settings preview")));
            File.WriteAllText(Path.Combine(pluginDirectory, $"{classPrefix}SettingsControl.xaml.cs"), PluginRepository.NormalizeLineEndings(BuildControlCodeBehind(namespaceSegment, classPrefix, "SettingsControl")));
        }

        if (archetype.HasRuntime)
            File.WriteAllText(Path.Combine(pluginDirectory, $"{classPrefix}Runtime.cs"), PluginRepository.NormalizeLineEndings(BuildRuntimeClass(namespaceSegment, classPrefix)));

        WriteResxFiles(pluginDirectory, classPrefix, request.DisplayName);
        File.WriteAllText(Path.Combine(testsDirectory, $"{classPrefix}PluginTests.cs"), PluginRepository.NormalizeLineEndings(BuildPluginTests(namespaceSegment, classPrefix, request, archetype)));
        File.WriteAllText(Path.Combine(testsDirectory, $"{classPrefix}TextTests.cs"), PluginRepository.NormalizeLineEndings(BuildTextTests(namespaceSegment, classPrefix)));

        string? storeEntryPath = null;
        if (request.Official)
        {
            storeEntryPath = Path.Combine(pluginDirectory, "store-entry.json");
            PluginRepository.WriteJsonFile(storeEntryPath, new OfficialStoreEntry(
                Description: description,
                Icon: DefaultIcon,
                IconBackground: "#FFF1E2",
                Tags: ["new-plugin", "official-candidate"],
                Dependencies: Array.Empty<string>(),
                SupportedLanguages: ["en", "zh-Hans"],
                RepositoryUrl: "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins"));
        }

        if (File.Exists(repository.SolutionPath))
        {
            await _processRunner.RunDotnetAsync(["sln", repository.SolutionPath, "add", pluginProjectPath, testProjectPath], repository.RootPath, log, cancellationToken);
        }

        log?.Invoke($"Created plugin scaffold at {pluginDirectory}");
        return new ScaffoldResult(pluginDirectory, testsDirectory, pluginProjectPath, testProjectPath, storeEntryPath);
    }

    public PromoteResult Promote(PromoteRequest request)
    {
        var repository = _repository.Load(request.RepositoryRoot);
        var pluginId = _repository.ResolveTargetPluginIds(repository, [request.PluginId]).Single();
        var plugin = repository.Plugins[pluginId];
        var storeEntryPath = Path.Combine(plugin.DirectoryPath, "store-entry.json");

        if (File.Exists(storeEntryPath) && !request.Overwrite)
            return new PromoteResult(storeEntryPath, Created: false);

        var storeEntry = new OfficialStoreEntry(
            Description: plugin.Manifest.Name,
            Icon: DefaultIcon,
            IconBackground: "#FFF1E2",
            Tags: ["official-candidate"],
            Dependencies: Array.Empty<string>(),
            SupportedLanguages: _repository.InferSupportedLanguages(plugin),
            RepositoryUrl: string.IsNullOrWhiteSpace(plugin.Manifest.Repository) ? null : plugin.Manifest.Repository);

        PluginRepository.WriteJsonFile(storeEntryPath, storeEntry);
        return new PromoteResult(storeEntryPath, Created: true);
    }

    private static void WriteResxFiles(string pluginDirectory, string classPrefix, string displayName)
    {
        var resourceDirectory = Path.Combine(pluginDirectory, "Resources");
        File.WriteAllText(Path.Combine(resourceDirectory, "Resource.resx"), PluginRepository.NormalizeLineEndings(BuildResourceFile(displayName, displayName, $"{displayName} Settings", $"{displayName} feature preview", $"{displayName} settings preview")));
        File.WriteAllText(Path.Combine(resourceDirectory, "Resource.en.resx"), PluginRepository.NormalizeLineEndings(BuildResourceFile(displayName, displayName, $"{displayName} Settings", $"{displayName} feature preview", $"{displayName} settings preview")));
        File.WriteAllText(Path.Combine(resourceDirectory, "Resource.zh-Hans.resx"), PluginRepository.NormalizeLineEndings(BuildResourceFile(displayName, displayName, $"{displayName} 设置", $"{displayName} 功能预览", $"{displayName} 设置预览")));
    }

    private static string BuildProjectFile(ScaffoldRequest request)
    {
        return $$"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <UseWPF>true</UseWPF>
    <Platforms>x64</Platforms>
    <Nullable>enable</Nullable>
    <Version>1.0.0</Version>
    <FileVersion>1.0.0</FileVersion>
    <AssemblyVersion>1.0.0</AssemblyVersion>
    <AssemblyName>LenovoLegionToolkit.Plugins.{{request.FolderName}}</AssemblyName>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <OutputPath>..\..\Build\plugins\LenovoLegionToolkit.Plugins.{{request.FolderName}}\</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <OutputPath>..\..\Build\plugins\LenovoLegionToolkit.Plugins.{{request.FolderName}}\</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\SDK\LenovoLegionToolkit.Plugins.SDK.csproj" />
    <ProjectReference Include="..\Shared\LenovoLegionToolkit.Plugins.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <ItemGroup>
    <Compile Update="Resources\Resource.Designer.cs">
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <DependentUpon>Resource.resx</DependentUpon>
    </Compile>
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Update="Resources\Resource.resx">
      <Generator>PublicResXFileCodeGenerator</Generator>
      <LastGenOutput>Resource.Designer.cs</LastGenOutput>
    </EmbeddedResource>
  </ItemGroup>

</Project>
""";
    }

    private static string BuildTestProjectFile(ScaffoldRequest request, string namespaceSegment)
    {
        return $$"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <RootNamespace>LenovoLegionToolkit.Plugins.{{namespaceSegment}}.Tests</RootNamespace>
    <AssemblyName>LenovoLegionToolkit.Plugins.{{namespaceSegment}}.Tests</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <AppendTargetFrameworkToOutputPath>true</AppendTargetFrameworkToOutputPath>
    <AppendRuntimeIdentifierToOutputPath>true</AppendRuntimeIdentifierToOutputPath>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    <GenerateDependencyFile>true</GenerateDependencyFile>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\{{request.FolderName}}\LenovoLegionToolkit.Plugins.{{request.FolderName}}.csproj" />
    <ProjectReference Include="..\..\SDK\LenovoLegionToolkit.Plugins.SDK.csproj" />
    <Compile Include="..\TestCommon\LocalizedTextTestsBase.cs" Link="TestCommon\LocalizedTextTestsBase.cs" />
    <Compile Include="..\TestCommon\PluginPageAssertions.cs" Link="TestCommon\PluginPageAssertions.cs" />
    <Reference Include="LenovoLegionToolkit.Lib">
      <HintPath>..\..\Dependencies\Host\LenovoLegionToolkit.Lib.dll</HintPath>
      <Private>true</Private>
    </Reference>
  </ItemGroup>

  <Target Name="CleanupPluginOutput" />

</Project>
""";
    }

    private static string BuildPluginJson(ScaffoldRequest request)
    {
        return $$"""
{
  "id": "{{request.PluginId}}",
  "name": "{{request.DisplayName}}",
  "version": "1.0.0",
  "minLLTVersion": "{{request.MinimumHostVersion}}",
  "author": "{{request.Author}}",
  "isSystemPlugin": false,
  "repository": "",
  "issues": ""
}
""";
    }

    private static string BuildPluginChangelog(string displayName)
    {
        return $$"""
# {{displayName}}

## [Unreleased]

### Added
- Initial plugin scaffold / 初始插件骨架
""";
    }

    private static string BuildTextClass(string namespaceSegment, string classPrefix, ScaffoldRequest request)
    {
        return $$"""
using LenovoLegionToolkit.Plugins.{{namespaceSegment}}.Resources;

namespace LenovoLegionToolkit.Plugins.{{namespaceSegment}};

public static class {{classPrefix}}Text
{
    public static string PluginName => Resource.PluginName;
    public static string SettingsPageTitle => Resource.SettingsPageTitle;
    public static string FeaturePageTitle => Resource.FeaturePageTitle;
    public static string FeaturePageDescription => Resource.FeaturePageDescription;
    public static string SettingsPageDescription => Resource.SettingsPageDescription;
}
""";
    }

    private static string BuildPluginClass(string namespaceSegment, string classPrefix, ScaffoldRequest request, string description, ArchetypeDefinition archetype)
    {
        var featureExtension = archetype.HasFeaturePage
            ? $"    public override object? GetFeatureExtension() => new {classPrefix}FeaturePage();{Environment.NewLine}"
            : string.Empty;
        var settingsExtension = archetype.HasSettingsPage
            ? $"    public override object? GetSettingsPage() => new {classPrefix}SettingsPage();{Environment.NewLine}"
            : string.Empty;
        var runtimeField = archetype.HasRuntime
            ? $"    private readonly {classPrefix}Runtime _runtime = new();{Environment.NewLine}{Environment.NewLine}"
            : string.Empty;
        var optimization = archetype.HasOptimizationCategory
            ? $$"""

    public override LenovoLegionToolkit.Lib.Optimization.WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
    {
        return new LenovoLegionToolkit.Lib.Optimization.WindowsOptimizationCategoryDefinition(
            "{{request.PluginId}}.optimization",
            "{{classPrefix}}_Optimization_Title",
            "{{classPrefix}}_Optimization_Description",
            new[]
            {
                new LenovoLegionToolkit.Lib.Optimization.WindowsOptimizationActionDefinition(
                    "{{request.PluginId}}.optimization.enable",
                    "{{classPrefix}}_Optimization_Enable_Title",
                    "{{classPrefix}}_Optimization_Enable_Description",
                    async _ => await _runtime.RunAsync().ConfigureAwait(false),
                    Recommended: true)
            },
            Id);
    }
"""
            : string.Empty;

        return $$"""
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.{{namespaceSegment}};

[Plugin(
    id: "{{request.PluginId}}",
    name: "{{request.DisplayName}}",
    version: "1.0.0",
    description: "{{description}}",
    author: "{{request.Author}}",
    MinimumHostVersion = "{{request.MinimumHostVersion}}",
    Icon = "{{DefaultIcon}}"
)]
public sealed class {{classPrefix}}Plugin : PluginBase
{
{{runtimeField}}    public override string Id => "{{request.PluginId}}";
    public override string Name => {{classPrefix}}Text.PluginName;
    public override string Description => "{{description}}";
    public override string Icon => "{{DefaultIcon}}";
    public override bool IsSystemPlugin => false;

{{featureExtension}}{{settingsExtension}}{{optimization}}
}

public sealed class {{classPrefix}}FeaturePage : IPluginPage
{
    public string PageTitle => {{classPrefix}}Text.FeaturePageTitle;
    public string? PageIcon => "{{DefaultIcon}}";

    public object CreatePage() => new {{classPrefix}}Control();
}

public sealed class {{classPrefix}}SettingsPage : IPluginPage
{
    public string PageTitle => {{classPrefix}}Text.SettingsPageTitle;
    public string? PageIcon => "Settings24";

    public object CreatePage() => new {{classPrefix}}SettingsControl();
}
""";
    }

    private static string BuildContentControlXaml(string controlPrefix, string title, string description)
    {
        return $$"""
<UserControl x:Class="LenovoLegionToolkit.Plugins.{{controlPrefix.Replace("Settings", string.Empty, StringComparison.Ordinal)}}.{{controlPrefix}}"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Padding="24"
            CornerRadius="20"
            Background="{DynamicResource ControlFillColorDefaultBrush}"
            BorderBrush="{DynamicResource ControlStrokeColorDefaultBrush}"
            BorderThickness="1">
        <StackPanel>
            <TextBlock FontSize="24"
                       FontWeight="SemiBold"
                       Text="{{title}}" />
            <TextBlock Margin="0,12,0,0"
                       TextWrapping="Wrap"
                       Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                       Text="{{description}}" />
        </StackPanel>
    </Border>
</UserControl>
""";
    }

    private static string BuildControlCodeBehind(string namespaceSegment, string classPrefix, string suffix)
    {
        return $$"""
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.{{namespaceSegment}};

public partial class {{classPrefix}}{{suffix}} : UserControl
{
    public {{classPrefix}}{{suffix}}()
    {
        InitializeComponent();
    }
}
""";
    }

    private static string BuildRuntimeClass(string namespaceSegment, string classPrefix)
    {
        return $$"""
namespace LenovoLegionToolkit.Plugins.{{namespaceSegment}};

public sealed class {{classPrefix}}Runtime
{
    public Task RunAsync()
    {
        return Task.CompletedTask;
    }
}
""";
    }

    private static string BuildPluginTests(string namespaceSegment, string classPrefix, ScaffoldRequest request, ArchetypeDefinition archetype)
    {
        return $$"""
using LenovoLegionToolkit.Plugins.{{namespaceSegment}};
using LenovoLegionToolkit.Plugins.TestCommon;
using Xunit;

namespace LenovoLegionToolkit.Plugins.{{namespaceSegment}}.Tests;

public class {{classPrefix}}PluginTests
{
    [Fact]
    public void Plugin_HasExpectedMetadata()
    {
        var plugin = new {{classPrefix}}Plugin();

        Assert.Equal("{{request.PluginId}}", plugin.Id);
        Assert.Equal({{classPrefix}}Text.PluginName, plugin.Name);
        Assert.Equal("{{request.MinimumHostVersion}}", typeof({{classPrefix}}Plugin).GetCustomAttributes(typeof(PluginAttribute), false).Cast<PluginAttribute>().Single().MinimumHostVersion);
    }

    [Fact]
    public void Plugin_Pages_AreAvailable()
    {
        var plugin = new {{classPrefix}}Plugin();

        {{(archetype.HasFeaturePage ? $"PluginPageAssertions.AssertPluginPage(plugin.GetFeatureExtension(), {classPrefix}Text.FeaturePageTitle, \"{DefaultIcon}\");" : "// No feature page for this archetype.")}}
        {{(archetype.HasSettingsPage ? $"PluginPageAssertions.AssertPluginPage(plugin.GetSettingsPage(), {classPrefix}Text.SettingsPageTitle, \"Settings24\");" : "// No settings page for this archetype.")}}
    }
}
""";
    }

    private static string BuildTextTests(string namespaceSegment, string classPrefix)
    {
        return $$"""
using LenovoLegionToolkit.Plugins.{{namespaceSegment}}.Resources;
using LenovoLegionToolkit.Plugins.TestCommon;

namespace LenovoLegionToolkit.Plugins.{{namespaceSegment}}.Tests;

public sealed class {{classPrefix}}TextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof({{classPrefix}}Text);
    protected override Type ResourceType => typeof(Resource);
    protected override string[] RequiredKeys => ["PluginName", "FeaturePageTitle", "SettingsPageTitle"];
}
""";
    }

    private static string BuildResourceFile(string pluginName, string featureTitle, string settingsTitle, string featureDescription, string settingsDescription)
    {
        return $$"""
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="PluginName" xml:space="preserve">
    <value>{{pluginName}}</value>
  </data>
  <data name="FeaturePageTitle" xml:space="preserve">
    <value>{{featureTitle}}</value>
  </data>
  <data name="SettingsPageTitle" xml:space="preserve">
    <value>{{settingsTitle}}</value>
  </data>
  <data name="FeaturePageDescription" xml:space="preserve">
    <value>{{featureDescription}}</value>
  </data>
  <data name="SettingsPageDescription" xml:space="preserve">
    <value>{{settingsDescription}}</value>
  </data>
</root>
""";
    }
}
