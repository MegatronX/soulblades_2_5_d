using Godot;

/// <summary>
/// A Resource that defines a single group of enemies (e.g., "3 Goblins").
/// This can be reused across different maps and encounter types.
/// </summary>
[GlobalClass]
public partial class EnemyParty : Resource
{
    [Export]
    public Godot.Collections.Array<PackedScene> Members { get; private set; }
}