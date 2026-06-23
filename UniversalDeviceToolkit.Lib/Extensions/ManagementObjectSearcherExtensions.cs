using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Extensions;

public static class ManagementObjectSearcherExtensions
{
    public static async Task<IEnumerable<ManagementBaseObject>> GetAsync(this ManagementObjectSearcher mos)
    {
        using var collection = mos.Get();
        return collection.Cast<ManagementBaseObject>().ToArray();
    }
}
