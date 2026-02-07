using Godot;

/// <summary>
/// Controls a death effect that clones the target's sprite and applies a slashing shader.
/// Attach this to a scene containing a Sprite3D with the slashing_death.gdshader material.
/// </summary>
[Tool]
[GlobalClass]
public partial class SlashingDeathEffect : Node3D, IDeathEffect
{
    [Export] private Sprite3D _effectSprite; 
    [Export] private float _duration = 1.5f;
    [Export] private float _sliceAngle = 45.0f;

    [Export]
    public bool Preview
    {
        get => false;
        set
        {
            if (value && _effectSprite != null)
                RunPreview();
        }
    }

    public void Configure(Node3D target)
    {
        // 1. Find the visual sprite on the target
        Sprite3D targetSprite = target as Sprite3D;
        if (targetSprite == null)
        {
            // Search children for the first visible Sprite3D
            foreach (var child in target.GetChildren())
            {
                if (child is Sprite3D s && s.Visible)
                {
                    targetSprite = s;
                    break;
                }
            }
        }

        if (targetSprite == null || _effectSprite == null)
        {
            // Fail gracefully: just destroy self, target handles its own cleanup
            QueueFree();
            return;
        }

        // 2. Create a clone of the target sprite
        // Duplicate() copies all properties (Texture, flags, etc.) automatically.
        var clone = (Sprite3D)targetSprite.Duplicate();
        AddChild(clone);
        clone.GlobalTransform = targetSprite.GlobalTransform;

        // Hide our template sprite, we use the clone for the effect
        _effectSprite.Visible = false;
        
        // 3. Setup Material
        // Use the material from our template as a base
        var mat = _effectSprite.MaterialOverride as ShaderMaterial;
        if (mat != null)
        {
            mat = (ShaderMaterial)mat.Duplicate();
            clone.MaterialOverride = mat;
            mat.SetShaderParameter("slice_angle", _sliceAngle);
            mat.SetShaderParameter("texture_albedo", clone.Texture);
        }

        // 4. Hide the original target (it's "dead" now, we are the visual corpse)
        targetSprite.Visible = false;

        // 5. Animate
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v => 
        {
            if (mat != null) mat.SetShaderParameter("progress", v);
        }), 0.0f, 1.0f, _duration);
        tween.Finished += QueueFree;
    }

    private void RunPreview()
    {
        if (_effectSprite.MaterialOverride is not ShaderMaterial mat)
        {
            GD.Print("Assign a ShaderMaterial to the Sprite3D's MaterialOverride to preview.");
            return;
        }

        if (_effectSprite.Texture != null)
        {
            mat.SetShaderParameter("texture_albedo", _effectSprite.Texture);
        }

        mat.SetShaderParameter("slice_angle", _sliceAngle);

        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(SetProgress), 0.0f, 1.0f, _duration);
        // Reset after preview so the sprite remains visible in the editor
        tween.TweenCallback(Callable.From(() => SetProgress(0.0f)));
    }

    private void SetProgress(float val)
    {
        if (_effectSprite.MaterialOverride is ShaderMaterial mat)
        {
            mat.SetShaderParameter("progress", val);
        }
    }
}
