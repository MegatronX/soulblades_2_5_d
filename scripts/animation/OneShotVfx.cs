using Godot;
using System.Threading.Tasks;

/// <summary>
/// A utility script for VFX scenes (like explosions or hits) that should play once and then destroy themselves.
/// Attach this to the root Node3D of your VFX scene.
/// </summary>
public partial class OneShotVfx : Node3D
{
    [Signal]
    public delegate void PlaybackFinishedEventHandler();

    [Export] public bool RandomizeRotation { get; set; } = false;
    [Export] public bool ForceSpriteBillboard { get; set; } = true;
    [Export] public AnimationPlayer _animationPlayer {get; set; } = null;
    [Export] public GpuParticles3D _particles {get; set; } = null;
    [Export] public AnimatedSprite3D _animatedSprite {get; set; } = null;
    
    private bool _didFinish = false;
    private readonly TaskCompletionSource<bool> _completionSource = new();
    public Task CompletionTask => _completionSource.Task;


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
            animPlayer.AnimationFinished += (animName) => FinishPlayback();

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
            if (ForceSpriteBillboard)
            {
                animatedSprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            }

            animatedSprite.AnimationFinished += FinishPlayback;
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
            particles.Finished += FinishPlayback;
            return;
        }
        
        // Fallback safety: destroy after 1 second if no controller found
        GetTree().CreateTimer(1.0f).Timeout += FinishPlayback;
    }

    public override void _ExitTree()
    {
        _completionSource.TrySetResult(true);
    }

    private void FinishPlayback()
    {
        if (_didFinish) return;
        _didFinish = true;
        EmitSignal(SignalName.PlaybackFinished);
        _completionSource.TrySetResult(true);
        QueueFree();
    }
}
