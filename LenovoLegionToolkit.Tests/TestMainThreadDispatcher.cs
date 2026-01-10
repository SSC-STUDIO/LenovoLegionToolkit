using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Tests;

public class TestMainThreadDispatcher : IMainThreadDispatcher
{
    public void Dispatch(Action callback) => callback();

    public Task DispatchAsync(Func<Task> callback) => callback();
}
