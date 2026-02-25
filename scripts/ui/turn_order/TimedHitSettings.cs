using Godot;

public enum TimedHitAnchorPoint
{
    ExecutionStart = 0,
    TravelStart = 1,
    TravelEnd = 2
}

public enum TimedHitOffsetMode
{
    Seconds = 0,
    NormalizedExecutionAnimation = 1
}

/// <summary>
/// Defines the timing and effects for a timed hit (action command) associated with an action.
/// </summary>
[GlobalClass]
public partial class TimedHitSettings : Resource
{
    [Export]
    public string WindowLabel { get; set; } = string.Empty;

    [Export]
    public TimedHitAnchorPoint AnchorPoint { get; set; } = TimedHitAnchorPoint.ExecutionStart;

    [Export]
    public TimedHitOffsetMode OffsetMode { get; set; } = TimedHitOffsetMode.Seconds;

    [Export] public float TimingOffset { get; set; } = 0.5f; // Seconds from start of animation to the perfect hit moment.
    [Export(PropertyHint.Range, "0,1,0.001")]
    public float TimingOffsetNormalized { get; set; } = 0.5f; // 0..1 position in execution animation when OffsetMode=NormalizedExecutionAnimation.
    [Export] public float VisualShrinkDuration { get; set; } = 1.5f; // How long the ring takes to shrink.
    [Export] public float WindowDuration { get; set; } = 0.6f; // Total duration the UI is visible.
    
    [Export] public float DamageMultiplier { get; set; } = 1.25f; // Multiplier applied on a Perfect/Great hit.

    [Export] public CameraEffect PerfectCameraEffect { get; set; }
    [Export] public CameraEffect GreatCameraEffect { get; set; }
}
