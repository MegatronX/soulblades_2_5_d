using Godot;

/// <summary>
/// A specialized ActionData for physical or magical attacks.
/// Simplifies creation by exposing common combat stats directly.
/// </summary>
[GlobalClass]
public partial class AttackData : ActionData
{
    [Export]
    public int Power { get; private set; } = 10;

    [Export]
    public int Accuracy { get; private set; } = 95;

    [Export]
    public ElementType Element { get; private set; } = ElementType.None;
}