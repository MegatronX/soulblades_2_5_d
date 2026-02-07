using Godot;

/// <summary>
/// A visual modification that applies a specific shader material to a character.
/// </summary>
[GlobalClass]
public partial class ShaderModification : VisualModification
{
    [Export]
    public ShaderMaterial ShaderMaterial { get; private set; }
}