using Godot;

/// <summary>
/// Weighted spawn entry for ambient props.
/// Enables per-prop frequency tuning and optional per-prop active limits.
/// </summary>
[GlobalClass]
public partial class AmbientPropSpawnEntry : Resource
{
    [Export]
    public PackedScene PropScene { get; private set; }

    [Export(PropertyHint.Range, "1,1000,1")]
    public int Weight { get; private set; } = 10;

    [Export(PropertyHint.Range, "0,256,1")]
    public int MaxActiveCount { get; private set; } = 0;
}
