using Godot;

/// <summary>
/// A visual modification that changes the scale of a character's sprite.
/// </summary>
[GlobalClass]
public partial class ScaleModification : VisualModification
{
    [Export]
    public Vector2 ScaleMultiplier { get; private set; } = Vector2.One;
}