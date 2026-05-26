using System.Collections.Generic;

namespace LenovoLegionToolkit.Lib.DeviceSupport;

public sealed record DeviceFeatureAvailability
{
    public bool IsSupported { get; init; }
    public bool IsBasicMode => !IsSupported;
    public string? DevicePackId { get; init; }
    public IReadOnlyCollection<string> EnabledFeatures { get; init; } = [];
    public IReadOnlyCollection<string> HiddenFeatures { get; init; } = [];
}
