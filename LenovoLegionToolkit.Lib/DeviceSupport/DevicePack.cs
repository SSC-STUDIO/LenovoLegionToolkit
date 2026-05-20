using System.Collections.Generic;

namespace LenovoLegionToolkit.Lib.DeviceSupport;

public sealed record DevicePack
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Vendor { get; init; }
    public IReadOnlyCollection<string> Families { get; init; } = [];
    public IReadOnlyCollection<string> ModelPrefixes { get; init; } = [];
    public IReadOnlyCollection<string> ModelKeywords { get; init; } = [];
    public IReadOnlyCollection<string> MachineTypes { get; init; } = [];
    public IReadOnlyCollection<string> EnabledFeatures { get; init; } = [];
    public IReadOnlyCollection<string> HiddenFeatures { get; init; } = [];
}
