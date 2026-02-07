using Godot;

/// <summary>
/// Controls the visual animation of the 3D target cursor using an AnimationPlayer.
/// Attach this to the root Node3D of your TargetIndicator scene.
/// </summary>
public partial class TargetIndicator : Node3D
{
    [Export]
    public AnimationPlayer AnimationPlayer { get; set; }

    public override void _Ready()
    {
        if (AnimationPlayer == null)
        {
            AnimationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        }

        if (AnimationPlayer != null)
        {
            // Automatically play Idle if it exists and isn't set to Autoplay
            if (AnimationPlayer.HasAnimation("Idle") && !AnimationPlayer.IsPlaying())
            {
                AnimationPlayer.Play("Idle");
            }
        }
    }
}
