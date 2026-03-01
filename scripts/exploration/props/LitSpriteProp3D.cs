using Godot;
using System.Collections.Generic;

/// <summary>
/// Optional Sprite3D material helper for exploration props.
/// Assign a normal map to enable per-pixel lighting depth on one or more sprite targets.
/// </summary>
[GlobalClass]
public partial class LitSpriteProp3D : Node3D
{
    [ExportCategory("Lit Sprite Prop")]
    [Export]
    public NodePath SpritePath { get; private set; } = "Sprite3D";

    [Export]
    public Godot.Collections.Array<NodePath> AdditionalSpritePaths { get; private set; } = new();

    [ExportGroup("Material Activation")]
    [Export]
    public bool ForceMaterialOverride { get; private set; } = false;

    [Export]
    public Texture2D AlbedoOverrideTexture { get; private set; }

    [Export]
    public Texture2D NormalMapTexture { get; private set; }

    [ExportGroup("Lighting")]
    [Export(PropertyHint.Range, "0,4,0.01")]
    public float NormalScale { get; private set; } = 1.0f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Roughness { get; private set; } = 1.0f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Specular { get; private set; } = 0.05f;

    [Export]
    public bool DisableBackfaceCulling { get; private set; } = true;

    [Export]
    public bool UseNearestTextureFilter { get; private set; } = true;

    [ExportGroup("Alpha")]
    [Export]
    public bool UseAlphaScissor { get; private set; } = true;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AlphaScissorThreshold { get; private set; } = 0.5f;

    public override void _Ready()
    {
        ApplyMaterial();
    }

    private void ApplyMaterial()
    {
        var sprites = ResolveTargetSprites();
        if (sprites.Count == 0)
        {
            return;
        }

        bool shouldOverride = ForceMaterialOverride || NormalMapTexture != null;
        if (!shouldOverride)
        {
            return;
        }

        foreach (var sprite in sprites)
        {
            if (sprite == null || !GodotObject.IsInstanceValid(sprite))
            {
                continue;
            }

            var albedo = AlbedoOverrideTexture ?? sprite.Texture;
            if (albedo == null)
            {
                continue;
            }

            ApplyMaterialToSprite(sprite, albedo);
        }
    }

    private void ApplyMaterialToSprite(Sprite3D sprite, Texture2D albedo)
    {
        sprite.Shaded = true;
        if (AlbedoOverrideTexture != null)
        {
            sprite.Texture = AlbedoOverrideTexture;
        }

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
            AlbedoTexture = albedo,
            VertexColorUseAsAlbedo = true,
            Roughness = Mathf.Clamp(Roughness, 0f, 1f),
            MetallicSpecular = Mathf.Clamp(Specular, 0f, 1f),
            CullMode = DisableBackfaceCulling
                ? BaseMaterial3D.CullModeEnum.Disabled
                : BaseMaterial3D.CullModeEnum.Back,
            TextureFilter = UseNearestTextureFilter
                ? BaseMaterial3D.TextureFilterEnum.Nearest
                : BaseMaterial3D.TextureFilterEnum.Linear
        };

        if (UseAlphaScissor)
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
            material.AlphaScissorThreshold = Mathf.Clamp(AlphaScissorThreshold, 0f, 1f);
        }

        if (NormalMapTexture != null)
        {
            material.NormalEnabled = true;
            material.NormalTexture = NormalMapTexture;
            material.NormalScale = Mathf.Clamp(NormalScale, 0f, 4f);
        }

        sprite.MaterialOverride = material;
    }

    private List<Sprite3D> ResolveTargetSprites()
    {
        var targets = new List<Sprite3D>();
        TryAddTargetSprite(targets, SpritePath);
        if (AdditionalSpritePaths != null)
        {
            foreach (NodePath path in AdditionalSpritePaths)
            {
                TryAddTargetSprite(targets, path);
            }
        }

        return targets;
    }

    private void TryAddTargetSprite(List<Sprite3D> targets, NodePath path)
    {
        if (path.IsEmpty) return;
        var sprite = GetNodeOrNull<Sprite3D>(path);
        if (sprite == null || !GodotObject.IsInstanceValid(sprite)) return;
        if (targets.Contains(sprite)) return;
        targets.Add(sprite);
    }
}
