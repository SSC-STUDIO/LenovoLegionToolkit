using System;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.WPF.Settings;

public class DashboardSettings() : AbstractSettings<DashboardSettings.DashboardSettingsStore>("dashboard.json")
{
    private const int CurrentSchemaVersion = 4;

    public class DashboardSettingsStore
    {
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public bool ShowSensors { get; set; } = true;
        public int SensorsRefreshIntervalSeconds { get; set; } = 1;
        public DashboardGroup[]? Groups { get; set; }
    }

    protected override DashboardSettingsStore Default => new();

    public override DashboardSettingsStore? LoadStore() => Normalize(base.LoadStore());

    public override async Task<DashboardSettingsStore?> LoadStoreAsync() =>
        Normalize(await base.LoadStoreAsync().ConfigureAwait(false));

    private static DashboardSettingsStore Normalize(DashboardSettingsStore? store)
    {
        var normalized = store ?? new DashboardSettingsStore();

        normalized.Groups = NormalizeGroups(normalized.Groups);

        // Ensure all groups have non-null items
        normalized.Groups = normalized.Groups
            .Select(group => new DashboardGroup(
                group.Type, 
                group.CustomName, 
                group.Items ?? Array.Empty<DashboardItem>()))
            .ToArray();

        var isLegacySchema = normalized.SchemaVersion < 3;
        if (isLegacySchema)
        {
            // Legacy dashboard settings predate the unified sensors card and the
            // current dashboard grouping. Repair those persisted layouts once.
            normalized.ShowSensors = true;
        }

        if (!normalized.Groups.SelectMany(group => group.Items).Contains(DashboardItem.PowerMode))
        {
            var groups = normalized.Groups.ToArray();
            var powerGroupIndex = System.Array.FindIndex(groups, group => group.Type == DashboardGroupType.Power);

            if (powerGroupIndex >= 0)
            {
                var powerGroup = groups[powerGroupIndex];
                var items = powerGroup.Items.Prepend(DashboardItem.PowerMode).Distinct().ToArray();
                groups[powerGroupIndex] = new DashboardGroup(powerGroup.Type, powerGroup.CustomName, items.ToArray());
            }
            else
            {
                groups = [new DashboardGroup(DashboardGroupType.Power, null, DashboardItem.PowerMode), .. groups];
            }

            normalized.Groups = groups;
        }

        if (!normalized.Groups.SelectMany(group => group.Items).Contains(DashboardItem.ItsMode))
        {
            var groups = normalized.Groups.ToArray();
            var powerGroupIndex = System.Array.FindIndex(groups, group => group.Type == DashboardGroupType.Power);

            if (powerGroupIndex >= 0)
            {
                var powerGroup = groups[powerGroupIndex];
                var items = powerGroup.Items.ToList();

                if (items.Contains(DashboardItem.PowerMode))
                {
                    var powerModeIndex = items.IndexOf(DashboardItem.PowerMode);
                    items.Insert(powerModeIndex + 1, DashboardItem.ItsMode);
                }
                else
                {
                    items.Insert(0, DashboardItem.ItsMode);
                }

                items = items.Distinct().ToList();
                groups[powerGroupIndex] = new DashboardGroup(powerGroup.Type, powerGroup.CustomName, items.ToArray());
            }
            else
            {
                groups = [new DashboardGroup(DashboardGroupType.Power, null, DashboardItem.ItsMode), .. groups];
            }

            normalized.Groups = groups;
        }

        normalized.SchemaVersion = CurrentSchemaVersion;
        return normalized;
    }

    private static DashboardGroup[] NormalizeGroups(DashboardGroup[]? groups)
    {
        if (groups is null)
            return DashboardGroup.DefaultGroups;

        return groups
            .Where(group => global::System.Enum.IsDefined(group.Type))
            .Select(group =>
            {
                var items = (group.Items ?? [])
                    .Where(global::System.Enum.IsDefined)
                    .Distinct()
                    .ToArray();

                return new DashboardGroup(group.Type, group.CustomName, items);
            })
            .ToArray();
    }
}
