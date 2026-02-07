using Godot;

/// <summary>
/// Defines the visual style of a battle arena, allowing reuse of assets from the world map.
/// </summary>
[GlobalClass]
public partial class BattleEnvironmentProfile : Resource
{
    [Export] public Color FloorColor { get; set; } = new Color("252530");
    [Export] public Texture2D FloorTexture { get; set; }
    [Export] public Vector2 FloorTiling { get; set; } = new Vector2(20, 20);

    [Export] public Color BackgroundColor { get; set; } = new Color("1a1a24");
    
    [Export] public float FloorYOffset { get; set; } = 0.0f;

    [Export] public Godot.Collections.Array<PackedScene> DecorationProps { get; set; } = new();
    [Export] public int DecorationCount { get; set; } = 20;

    [Export] public float DecorationMinRadius { get; set; } = 8.0f;
    [Export] public float DecorationMaxRadius { get; set; } = 25.0f;
    [Export] public float CameraExclusionAngle { get; set; } = 60.0f; // Degrees to keep clear towards camera (+Z)
}