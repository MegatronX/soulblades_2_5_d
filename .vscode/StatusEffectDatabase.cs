using Godot;

/// <summary>
/// A Resource that acts as a database for all StatusEffect resources in the game.
/// This allows us to load all effects from a single point and access them by a key.
/// </summary>
[GlobalClass]
public partial class StatusEffectDatabase : Resource
{
    [Export]
    public Godot.Collections.Dictionary<string, StatusEffect> Effects { get; private set; } = new();
}