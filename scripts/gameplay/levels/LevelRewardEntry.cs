using Godot;

/// <summary>
/// Defines abilities/actions learned at a specific level.
/// </summary>
[GlobalClass]
public partial class LevelRewardEntry : Resource
{
    [Export(PropertyHint.Range, "1,999,1")]
    public int Level { get; set; } = 1;

    [Export]
    public Godot.Collections.Array<Ability> Abilities { get; set; } = new();

    [Export(PropertyHint.ResourceType, "ActionData")]
    public Godot.Collections.Array<ActionData> Actions { get; set; } = new();
}
