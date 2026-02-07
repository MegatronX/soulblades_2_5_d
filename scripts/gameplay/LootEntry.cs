using Godot;

/// <summary>
/// Defines a single loot entry with a chance percentage.
/// </summary>
[GlobalClass]
public partial class LootEntry : Resource
{
    [Export]
    public ItemData Item { get; set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float ChancePercent { get; set; } = 100.0f;
}
