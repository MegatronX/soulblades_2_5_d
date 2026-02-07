using Godot;

/// <summary>
/// A Resource defining a single action a character can take in battle.
/// </summary>
[GlobalClass]
public partial class Action : Resource
{
    [Export]
    public string ActionName { get; private set; }

    [Export]
    public float TickCost { get; private set; } = 0; // A cost of 0 means a full reset.
}