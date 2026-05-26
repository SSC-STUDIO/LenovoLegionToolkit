using System;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Utils;

public interface IDelayProvider
{
    Task Delay(TimeSpan delay, CancellationToken token);
}
