using Godot;

/// <summary>
/// Defines the timing and effects for a timed hit (action command) associated with an action.
/// </summary>
[GlobalClass]
public partial class TimedHitSettings : Resource
{
    [Export] public float TimingOffset { get; set; } = 0.5f; // Seconds from start of animation to the perfect hit moment.
    [Export] public float VisualShrinkDuration { get; set; } = 1.5f; // How long the ring takes to shrink.
    [Export] public float WindowDuration { get; set; } = 0.6f; // Total duration the UI is visible.
    
    [Export] public float DamageMultiplier { get; set; } = 1.25f; // Multiplier applied on a Perfect/Great hit.

    [Export] public CameraEffect PerfectCameraEffect { get; set; }
    [Export] public CameraEffect GreatCameraEffect { get; set; }
}