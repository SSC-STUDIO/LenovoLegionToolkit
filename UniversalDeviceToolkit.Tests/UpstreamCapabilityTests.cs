using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

public sealed class UpstreamCapabilityTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), $"udt-upstream-{Guid.NewGuid():N}");
    private readonly string? _previous;

    public UpstreamCapabilityTests()
    {
        Directory.CreateDirectory(_temp);
        _previous = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _temp);
    }

    [Fact]
    public async Task OrTrigger_MatchesWhenAnyChildMatchesAndShortCircuits()
    {
        var first = new FakeTrigger(false);
        var second = new FakeTrigger(true);
        var third = new FakeTrigger(true);
        var trigger = new OrAutomationPipelineTrigger([first, second, third]);

        (await trigger.IsMatchingEvent(new FakeEvent())).Should().BeTrue();
        first.EventCalls.Should().Be(1);
        second.EventCalls.Should().Be(1);
        third.EventCalls.Should().Be(0);
    }

    [Fact]
    public void OrTrigger_DeepCopyCopiesChildren()
    {
        var child = new FakeTrigger(true);
        var copy = (OrAutomationPipelineTrigger)new OrAutomationPipelineTrigger([child]).DeepCopy();
        copy.Triggers.Should().ContainSingle();
        copy.Triggers[0].Should().NotBeSameAs(child);
    }

    [Fact]
    public void SettingsBackup_ImportCreatesRollbackAndRestoresJson()
    {
        File.WriteAllText(Path.Combine(_temp, "settings.json"), "{\"value\":1}");
        var service = new SettingsBackupService();
        var backup = Path.Combine(_temp, "export.udtbackup");
        service.Export(backup);
        File.WriteAllText(Path.Combine(_temp, "settings.json"), "{\"value\":2}");
        // Extra file present only after export — import must remove it (replace, not merge).
        File.WriteAllText(Path.Combine(_temp, "orphan.json"), "{\"orphan\":true}");

        var rollback = service.Import(backup);

        File.ReadAllText(Path.Combine(_temp, "settings.json")).Should().Contain("1");
        File.Exists(Path.Combine(_temp, "orphan.json")).Should().BeFalse();
        File.Exists(rollback).Should().BeTrue();
    }

    [Fact]
    public void SettingsBackup_RejectsFutureFormat()
    {
        var backup = Path.Combine(_temp, "future.udtbackup");
        using (var archive = ZipFile.Open(backup, ZipArchiveMode.Create))
        using (var writer = new StreamWriter(archive.CreateEntry("udt-settings-backup.json").Open()))
            writer.Write("{\"FormatVersion\":999,\"CreatedAtUtc\":\"2026-07-11T00:00:00Z\"}");

        new SettingsBackupService().Invoking(service => service.Import(backup)).Should().Throw<NotSupportedException>();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _previous);
        try { Directory.Delete(_temp, true); } catch { }
    }

    private sealed class FakeEvent : IAutomationEvent;
    private sealed class FakeTrigger(bool result) : IAutomationPipelineTrigger
    {
        public int EventCalls { get; private set; }
        public string DisplayName => "fake";
        public Task<bool> IsMatchingEvent(IAutomationEvent automationEvent) { EventCalls++; return Task.FromResult(result); }
        public Task<bool> IsMatchingState() => Task.FromResult(result);
        public void UpdateEnvironment(AutomationEnvironment environment) { }
        public IAutomationPipelineTrigger DeepCopy() => new FakeTrigger(result);
    }
}