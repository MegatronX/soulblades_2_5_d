using Godot;

/// <summary>
/// Defines all stat increments that occur at a specific level.
/// </summary>
[GlobalClass]
public partial class LevelStatIncrementEntry : Resource
{
    [Export(PropertyHint.Range, "1,999,1")]
    public int Level { get; set; } = 1;

    [Export]
    public Godot.Collections.Array<StatIncrement> Increments { get; set; } = new();
}
