using Autofac;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib;

public static class IoCContainer
{
    private static readonly object Lock = new();

    private static IContainer? _container;

    public static void Initialize(params Module[] modules)
    {
        lock (Lock)
        {
            if (_container is not null)
                throw ExceptionHelper.IoCAlreadyInitialized();

            var cb = new ContainerBuilder();

            foreach (var module in modules)
                cb.RegisterModule(module);

            _container = cb.Build();
        }
    }

    public static T Resolve<T>() where T : notnull
    {
        lock (Lock)
        {
            if (_container is null)
                throw ExceptionHelper.IoCMustBeInitialized(nameof(T));
            return _container.Resolve<T>();
        }
    }

    public static T? TryResolve<T>() where T : class
    {
        lock (Lock)
        {
            if (_container is null)
                return null;

            _ = _container.TryResolve(out T? value);
            return value;
        }
    }

}
