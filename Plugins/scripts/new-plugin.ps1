param(
    [Parameter(Mandatory = $true)]
    [string]$FolderName,
    [Parameter(Mandatory = $true)]
    [string]$PluginId,
    [Parameter(Mandatory = $true)]
    [string]$DisplayName,
    [string]$Author = $env:USERNAME,
    [string]$Description = "",
    [string]$MinimumHostVersion = "3.6.1",
    [string]$NamespaceSegment = "",
    [string]$ClassPrefix = ""
)

$ErrorActionPreference = "Stop"

function Get-SafeIdentifier {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $parts = [regex]::Matches($Value, "[A-Za-z0-9]+") | ForEach-Object {
        if ($_.Value.Length -eq 0) { return }
        $_.Value.Substring(0, 1).ToUpperInvariant() + $_.Value.Substring(1)
    }

    $identifier = ($parts -join "")
    if ([string]::IsNullOrWhiteSpace($identifier)) {
        throw "Unable to derive a valid identifier from '$Value'."
    }

    if ([char]::IsDigit($identifier[0])) {
        $identifier = "Plugin$identifier"
    }

    return $identifier
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$templateDir = Join-Path $repoRoot "Plugins\Template"
$pluginsRoot = Join-Path $repoRoot "Plugins"
$targetDir = Join-Path $pluginsRoot $FolderName
$testDir = Join-Path $pluginsRoot "$FolderName.Tests"

if (-not (Test-Path $templateDir)) {
    throw "Template directory not found at $templateDir"
}

if (Test-Path $targetDir) {
    throw "Target plugin directory already exists: $targetDir"
}

if (Test-Path $testDir) {
    throw "Target test directory already exists: $testDir"
}

if ([string]::IsNullOrWhiteSpace($Description)) {
    $Description = "$DisplayName plugin for Lenovo Legion Toolkit"
}

if ([string]::IsNullOrWhiteSpace($NamespaceSegment)) {
    $NamespaceSegment = Get-SafeIdentifier -Value $FolderName
}

if ([string]::IsNullOrWhiteSpace($ClassPrefix)) {
    $ClassPrefix = $NamespaceSegment
}

Copy-Item -Path $templateDir -Destination $targetDir -Recurse

$replacements = [ordered]@{
    "LenovoLegionToolkit.Plugins.Template" = "LenovoLegionToolkit.Plugins.$NamespaceSegment"
    "LenovoLegionToolkit.Plugins.Template.csproj" = "LenovoLegionToolkit.Plugins.$FolderName.csproj"
    "<PluginFolderName>Template</PluginFolderName>" = "<PluginFolderName>$FolderName</PluginFolderName>"
    "id: `"myplugin`"" = "id: `"$PluginId`""
    "public override string Id => `"myplugin`";" = "public override string Id => `"$PluginId`";"
    "<PluginId>myplugin</PluginId>" = "<PluginId>$PluginId</PluginId>"
    "name: `"My Plugin`"" = "name: `"$DisplayName`""
    "public override string Name => `"My Plugin`";" = "public override string Name => `"$DisplayName`";"
    "<PluginName>My Plugin</PluginName>" = "<PluginName>$DisplayName</PluginName>"
    "description: `"My custom plugin for Lenovo Legion Toolkit`"" = "description: `"$Description`""
    "public override string Description => `"My custom plugin for Lenovo Legion Toolkit`";" = "public override string Description => `"$Description`";"
    "<PluginDescription>My custom plugin for Lenovo Legion Toolkit</PluginDescription>" = "<PluginDescription>$Description</PluginDescription>"
    "author: `"Your Name`"" = "author: `"$Author`""
    "MinimumHostVersion = `"3.6.1`"" = "MinimumHostVersion = `"$MinimumHostVersion`""
    "MyPluginTemplate" = "$ClassPrefix"
    "Feature Page Title" = "$DisplayName"
    "Settings Page Title" = "$DisplayName Settings"
    "Hello from My Plugin!" = "Hello from $DisplayName!"
}

$textExtensions = @(".csproj", ".cs", ".xaml", ".md", ".resx")
$pluginFiles = Get-ChildItem -Path $targetDir -Recurse -File
foreach ($file in $pluginFiles) {
    if ($textExtensions -notcontains $file.Extension) {
        continue
    }

    $content = Get-Content $file.FullName -Raw
    foreach ($replacement in $replacements.GetEnumerator()) {
        $content = $content.Replace($replacement.Key, $replacement.Value)
    }
    Set-Content -Path $file.FullName -Value $content -Encoding UTF8
}

$renameMap = @{
    "LenovoLegionToolkit.Plugins.Template.csproj" = "LenovoLegionToolkit.Plugins.$FolderName.csproj"
    "MyPluginTemplate.cs" = "$ClassPrefix.cs"
    "MyPluginTemplateControl.xaml" = "$ClassPrefix`Control.xaml"
    "MyPluginTemplateControl.xaml.cs" = "$ClassPrefix`Control.xaml.cs"
    "MyPluginTemplatePage.xaml.cs" = "$ClassPrefix`Page.xaml.cs"
    "MyPluginTemplateSettingsControl.xaml" = "$ClassPrefix`SettingsControl.xaml"
    "MyPluginTemplateSettingsControl.xaml.cs" = "$ClassPrefix`SettingsControl.xaml.cs"
    "MyPluginTemplateSettingsPage.xaml.cs" = "$ClassPrefix`SettingsPage.xaml.cs"
}

foreach ($entry in $renameMap.GetEnumerator()) {
    $sourcePath = Join-Path $targetDir $entry.Key
    if (Test-Path $sourcePath) {
        Rename-Item -Path $sourcePath -NewName $entry.Value
    }
}

$pluginJson = @"
{
  "id": "$PluginId",
  "name": "$DisplayName",
  "version": "1.0.0",
  "minLLTVersion": "$MinimumHostVersion",
  "author": "$Author",
  "isSystemPlugin": false,
  "repository": "",
  "issues": ""
}
"@

Set-Content -Path (Join-Path $targetDir "plugin.json") -Value $pluginJson -Encoding UTF8

$testProjectName = "$FolderName.Tests.csproj"
$testSourceName = "$ClassPrefix`PluginTests.cs"
$testProject = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <RootNamespace>LenovoLegionToolkit.Plugins.$NamespaceSegment.Tests</RootNamespace>
    <AssemblyName>LenovoLegionToolkit.Plugins.$NamespaceSegment.Tests</AssemblyName>
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
    <ProjectReference Include="..\$FolderName\LenovoLegionToolkit.Plugins.$FolderName.csproj" />
    <ProjectReference Include="..\..\SDK\LenovoLegionToolkit.Plugins.SDK.csproj" />
    <Reference Include="LenovoLegionToolkit.Lib">
      <HintPath>..\..\Dependencies\Host\LenovoLegionToolkit.Lib.dll</HintPath>
      <Private>true</Private>
    </Reference>
  </ItemGroup>

  <Target Name="CleanupPluginOutput" />

</Project>
"@

$testSource = @"
using System.Reflection;
using LenovoLegionToolkit.Plugins.SDK;
using Xunit;

namespace LenovoLegionToolkit.Plugins.$NamespaceSegment.Tests;

public class $ClassPrefix`PluginTests
{
    [Fact]
    public void Plugin_HasExpectedAttribute()
    {
        var attribute = typeof($ClassPrefix).GetCustomAttribute<PluginAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("$PluginId", attribute!.Id);
        Assert.Equal("$DisplayName", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);
        Assert.Equal("$MinimumHostVersion", attribute.MinimumHostVersion);
    }
}
"@

New-Item -ItemType Directory -Path $testDir | Out-Null
Set-Content -Path (Join-Path $testDir $testProjectName) -Value $testProject -Encoding UTF8
Set-Content -Path (Join-Path $testDir $testSourceName) -Value $testSource -Encoding UTF8

Write-Host "Created plugin scaffold:"
Write-Host "  Plugin: $targetDir"
Write-Host "  Tests:  $testDir"
Write-Host "Next steps:"
Write-Host "  1. Review generated UI/resources, plugin.json, and project/plugin version metadata."
Write-Host "  2. Add or update the generated plugin CHANGELOG.md before publishing from this repo."
Write-Host "  3. If publishing from this repo, update store.json and root CHANGELOG.md, then run: powershell -ExecutionPolicy Bypass -File .\Scripts\plugin-completion-check.ps1 -PluginIds $PluginId -OutputJson artifacts\plugin-completion-check-latest.json"
