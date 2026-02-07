using Godot;

/// <summary>
/// A utility script for VFX scenes (like explosions or hits) that should play once and then destroy themselves.
/// Attach this to the root Node3D of your VFX scene.
/// </summary>
public partial class OneShotVfx : Node3D
{
    [Export] public bool RandomizeRotation { get; set; } = false;
    [Export] public AnimationPlayer _animationPlayer {get; set; } = null;
    [Export] public GpuParticles3D _particles {get; set; } = null;
    [Export] public AnimatedSprite3D _animatedSprite {get; set; } = null;


    public override void _Ready()
    {
        if (RandomizeRotation)
        {
            Rotation = new Vector3(Rotation.X, (float)GD.RandRange(0, Mathf.Tau), Rotation.Z);
        }

        var animPlayer = _animationPlayer ?? GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animPlayer != null)
        {
            // Hook into the finished signal to clean up
            animPlayer.AnimationFinished += (animName) => QueueFree();

            // If not autoplaying, play the first animation found
            if (!animPlayer.IsPlaying())
            {
                var animList = animPlayer.GetAnimationList();
                if (animList.Length > 0)
                {
                    animPlayer.Play(animList[0]);
                }
            }
            return;
        }

        var animatedSprite = _animatedSprite ?? GetNodeOrNull<AnimatedSprite3D>("AnimatedSprite3D");
        if (animatedSprite != null)
        {
            animatedSprite.AnimationFinished += QueueFree;
            if (!animatedSprite.IsPlaying())
            {
                animatedSprite.Play();
            }
            return;
        }

        var particles = _particles ?? GetNodeOrNull<GpuParticles3D>("GPUParticles3D");
        if (particles != null)
        {
            particles.OneShot = true;
            particles.Emitting = true;
            particles.Finished += QueueFree;
            return;
        }
        
        // Fallback safety: destroy after 1 second if no controller found
        GetTree().CreateTimer(1.0f).Timeout += QueueFree;
    }
}
