namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Optional plugin lifecycle hook invoked after the application has loaded plugins.
/// Implement to start background work that should run for the app's lifetime.
/// </summary>
public interface IAppStartupPlugin
{
    /// <summary>
    /// Called when the host application has finished loading plugins.
    /// </summary>
    void OnAppStarted();
}
