using Godot;

/// <summary>
/// Basic NPC interactable shell.
/// Dialogue/cutscene routing can be layered on top later via effects.
/// </summary>
[GlobalClass]
public partial class NpcInteractable : ExplorationInteractableBase
{
    [Export]
    public string NpcId { get; private set; } = "npc";

    [ExportGroup("Visual Grounding")]
    [Export]
    public NodePath SpritePath { get; private set; } = "Sprite3D";

    [Export]
    public bool AutoAlignSpriteFeetToOrigin { get; private set; } = true;

    [Export(PropertyHint.Range, "-1,1,0.001")]
    public float SpriteFootLiftWorld { get; private set; } = 0f;

    [Export]
    public bool SnapToGroundOnReady { get; private set; } = true;

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float GroundSnapRaycastUp { get; private set; } = 1.5f;

    [Export(PropertyHint.Range, "0.1,20,0.01")]
    public float GroundSnapRaycastDown { get; private set; } = 6f;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint GroundSnapCollisionMask { get; private set; } = 1;

    public override void _Ready()
    {
        base._Ready();
        ApplyVisualGrounding();
    }

    protected override void OnBeforeEffects(ExplorationInteractionContext context)
    {
        GD.Print($"[NPC] Interacted with {NpcId}");
    }

    private void ApplyVisualGrounding()
    {
        if (AutoAlignSpriteFeetToOrigin)
        {
            AlignSpriteFeetToOrigin();
        }

        if (SnapToGroundOnReady)
        {
            SnapNodeToGround();
        }
    }

    private void AlignSpriteFeetToOrigin()
    {
        var sprite = GetNodeOrNull<Sprite3D>(SpritePath);
        if (sprite == null || sprite.Texture == null) return;

        if (!sprite.Centered)
        {
            // This alignment assumes centered sprites where origin is at mid-quad.
            return;
        }

        int frameHeight = Mathf.Max(1, sprite.Texture.GetHeight());
        if (sprite.Vframes > 1)
        {
            frameHeight = Mathf.Max(1, frameHeight / sprite.Vframes);
        }

        float pixelSize = Mathf.Max(0.0001f, sprite.PixelSize);
        float liftPixels = SpriteFootLiftWorld / pixelSize;
        float desiredOffsetY = (frameHeight * 0.5f) + liftPixels;

        Vector2 offset = sprite.Offset;
        offset.Y = desiredOffsetY;
        sprite.Offset = offset;
    }

    private void SnapNodeToGround()
    {
        var world = GetWorld3D();
        if (world == null) return;

        Vector3 start = GlobalPosition + Vector3.Up * Mathf.Max(0f, GroundSnapRaycastUp);
        Vector3 end = GlobalPosition + Vector3.Down * Mathf.Max(0.1f, GroundSnapRaycastDown);

        var query = PhysicsRayQueryParameters3D.Create(start, end, GroundSnapCollisionMask);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;

        var result = world.DirectSpaceState.IntersectRay(query);
        if (result.Count == 0) return;
        if (!result.ContainsKey("position")) return;

        Vector3 hitPos = (Vector3)result["position"];
        GlobalPosition = new Vector3(GlobalPosition.X, hitPos.Y, GlobalPosition.Z);
    }
}
