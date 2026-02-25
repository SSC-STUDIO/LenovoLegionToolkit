using System;

namespace LenovoLegionToolkit.Plugins.SDK;

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
        Version = version;
        Description = description;
        Author = author;
    }
}





