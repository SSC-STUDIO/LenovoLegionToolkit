using System;

namespace UniversalDeviceToolkit.Plugins.SDK;

/// <summary>
/// Plugin attribute used to mark a class as a plugin
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class PluginAttribute : Attribute
{
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public string Author { get; }
    public string MinimumHostVersion { get; set; } = "1.0.0";
    public string Icon { get; set; } = "Apps24";

    public PluginAttribute(string id, string name, string version, string description, string author)
    {
        Id = id;
        Name = name;

        if (!IsPluginVersion(version))
        {
            throw new ArgumentException($"'{version}' is not a valid version string.", nameof(version));
        }

        Version = version;

        Description = description;
        Author = author;
    }

    private static bool IsPluginVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        if (System.Version.TryParse(version, out _))
            return true;

        var hyphen = version.IndexOf('-');
        return hyphen > 0 &&
            hyphen < version.Length - 1 &&
            System.Version.TryParse(version[..hyphen], out _);
    }
}
