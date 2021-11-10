using System;

namespace Robust.Client.Mapping;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PlacementModeNameAttribute : Attribute
{
    public string Name { get; }

    public PlacementModeNameAttribute(string name)
    {
        Name = name;
    }
}
