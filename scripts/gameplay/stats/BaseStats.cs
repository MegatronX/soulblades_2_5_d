using Godot;

/// <summary>
/// A Resource that defines a complete set of base stats for a character archetype (e.g., "Goblin Stats").
/// This allows for defining all base stats in a single, reusable asset.
/// </summary>
[GlobalClass]
public partial class BaseStats : Resource
{
    [Export] public int HP { get; set; }
    [Export] public int MP { get; set; }
    [Export] public int Strength { get; set; }
    [Export] public int Defense { get; set; }
    [Export] public int Magic { get; set; }
    [Export] public int MagicDefense { get; set; }
    [Export] public int Speed { get; set; }
    [Export] public int Evasion { get; set; }
    [Export] public int MgEvasion { get; set; }
    [Export] public int Accuracy { get; set; }
    [Export] public int MgAccuracy { get; set; }
    [Export] public int Luck { get; set; }
    [Export] public int AP { get; set; }
}