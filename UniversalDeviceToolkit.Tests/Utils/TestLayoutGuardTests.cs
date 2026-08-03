using System.Text.RegularExpressions;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class TestLayoutGuardTests
{
    private static readonly Regex FileScopedNamespace = new(
        @"(?m)^namespace\s+(?<name>[A-Za-z0-9_.]+)\s*;",
        RegexOptions.Compiled);

    private static readonly Regex BlockNamespace = new(
        @"(?m)^namespace\s+(?<name>[A-Za-z0-9_.]+)\s*\{",
        RegexOptions.Compiled);

    private static readonly Regex TypeDeclaration = new(
        @"(?m)\b(?:class|struct|record|interface)\s+(?<name>[A-Za-z0-9_]+)",
        RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string[]> AggregateTypeContracts =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["FanTableInfoTests.cs"] = ["FanTableInfoStructTests"],
            ["MacroTests.cs"] = [
                "MacroEventTests", "MacroIdentifierTests", "MacroSequenceTests",
                "MacroControllerCleanUpTests", "MacroControllerEnabledTests", "MacroControllerAllowedKeysTests"],
            ["WindowsOptimizationRollbackTests.cs"] = [
                "WindowsOptimizationActionDefinitionContractTests",
                "WindowsOptimizationActionDefinitionSnapshotTests"],
            ["Controllers/SensorsControllerTests.cs"] = [
                "SensorsDataTests", "SensorDataTests", "ISensorsControllerTests", "GenericSensorsControllerTests"],
            ["Features/FeatureTests.cs"] = [
                "IFeatureTests", "BatteryStateTests", "PowerModeStateTests", "HybridModeStateTests",
                "GPUStateTests", "FanTableTypeTests"],
            ["Network/NetworkAccelerationFoundationTests.cs"] = [
                "HostsMarkedBlockTests", "PacFileGeneratorTests", "NetworkAccelerationConfigDefaultsTests",
                "DomainMatcherTests", "NetworkProxySessionTokenTests", "NetworkStateRecoveryServiceTests",
                "PacDomainMatchingIntegrationTests", "BuiltinDomainGroupsDefaultsTests", "NetworkDomainGroupMigrationTests",
                "NetworkAccelerationStartSafetyTests"],
            ["Settings/MoreSettingsStoreTests.cs"] = [
                "FanCurveSettingsStoreTests", "SpectrumKeyboardSettingsStoreTests"],
            ["Settings/PluginInfrastructureTests.cs"] = [
                "PluginManifestAdapterTests", "TestDataGeneratorTests", "AsyncTestHelpersTests",
                "TestAssertionsTests", "MockFactoryTests"],
            ["Settings/SettingsTestCollection.cs"] = [
                "LocalizationTestCollectionDefinition", "SettingsTestCollectionDefinition",
                "FlaUITestCollectionDefinition", "ProcessStateTestCollectionDefinition"],
            ["Utils/ThrottleDispatcherEdgeCaseTests.cs"] = [
                "ThrottleFirstDispatcherEdgeCaseTests", "ThrottleLastDispatcherEdgeCaseTests"]
        };

    [Fact]
    public void TestFiles_ShouldMatchDirectoryNamespaceAndTypeName()
    {
        var root = Path.Combine(RepositoryPaths.FindRoot(), "UniversalDeviceToolkit.Tests");
        var failures = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !IsBuildOutput(path)))
        {
            var source = File.ReadAllText(file);
            var relative = Path.GetRelativePath(root, file);
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (fileName is "AssemblyInfo" or "GlobalUsings")
                continue;

            var namespaceMatch = FileScopedNamespace.Match(source);
            if (!namespaceMatch.Success)
                namespaceMatch = BlockNamespace.Match(source);

            if (!namespaceMatch.Success)
            {
                failures.Add($"{relative}: missing namespace declaration");
                continue;
            }

            var directory = Path.GetDirectoryName(relative);
            var expectedNamespace = "UniversalDeviceToolkit.Tests";
            if (!string.IsNullOrWhiteSpace(directory))
            {
                expectedNamespace += "." + directory.Replace(Path.DirectorySeparatorChar, '.');
            }

            var actualNamespace = namespaceMatch.Groups["name"].Value;
            if (!actualNamespace.Equals(expectedNamespace, StringComparison.Ordinal))
            {
                failures.Add($"{relative}: namespace '{actualNamespace}' != '{expectedNamespace}'");
            }

            var typeNames = TypeDeclaration.Matches(source)
                .Select(match => match.Groups["name"].Value)
                .ToHashSet(StringComparer.Ordinal);
            var normalizedRelative = relative.Replace(Path.DirectorySeparatorChar, '/');
            if (!typeNames.Contains(fileName)
                && (!AggregateTypeContracts.TryGetValue(normalizedRelative, out var expectedTypes)
                    || expectedTypes.Any(typeName => !typeNames.Contains(typeName))))
            {
                failures.Add($"{relative}: no type named '{fileName}'");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void TestFiles_ShouldNotUseTemporaryPhaseNames()
    {
        var root = Path.Combine(RepositoryPaths.FindRoot(), "UniversalDeviceToolkit.Tests");
        var temporaryNames = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Select(path => Path.GetFileName(path))
            .Where(name => Regex.IsMatch(name, @"^Phase(?:[0-9A-Z]+)", RegexOptions.IgnoreCase))
            .ToArray();

        temporaryNames.Should().BeEmpty("tests should be named after the domain they verify");
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                         || part.Equals("obj", StringComparison.OrdinalIgnoreCase));
}
