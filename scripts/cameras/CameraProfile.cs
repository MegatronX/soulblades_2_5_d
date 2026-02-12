using Godot;

/// <summary>
/// Defines the behavior and intensity of the BattleCamera during action sequences.
/// </summary>
[GlobalClass]
public partial class CameraProfile : Resource
{
    [System.Flags]
    public enum CameraShotOptions
    {
        None = 0,
        DutchAngle = 1 << 0,
        DollyZoom = 1 << 1,
        ParallaxSweep = 1 << 2,
        PushIn = 1 << 3,
        WhipPan = 1 << 4,
        OrbitArc = 1 << 5,
        ImpactSnap = 1 << 6,
        RackFocus = 1 << 7,
        SpeedRamp = 1 << 8,
        PreStrike = 1 << 9
    }

    [Export] public bool EnableDynamicFraming { get; set; } = true;
    
    [ExportGroup("Action Framing")]
    [Export] public float ActionZoomFov { get; set; } = 35.0f; // Lower FOV for cinematic zoom
    [Export] public float FramingDistance { get; set; } = 8.0f; // How close the camera gets to the action
    [Export] public float FramingHeight { get; set; } = 2.0f; // Height offset during action
    [Export] public CameraShotOptions AllowedShots { get; set; } = CameraShotOptions.None;
    [Export] public float MaxDutchAngle { get; set; } = 5.0f; // Max tilt in degrees

    [Export] public float TransitionSpeed { get; set; } = 8.0f; // Speed of the zoom/pan
    
    [ExportGroup("Feel")]
    [Export] public float ShakeMultiplier { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0.0, 1.0")] public float TargetFollowRatio { get; set; } = 0.3f;

    [ExportGroup("Parallax Sweep")]
    [Export] public float ParallaxSweepAngle { get; set; } = 15.0f;
    [Export] public float ParallaxSweepSeconds { get; set; } = 2.0f;

    [ExportGroup("Orbit Arc")]
    [Export] public float OrbitArcAngle { get; set; } = 12.0f;
    [Export] public float OrbitArcSeconds { get; set; } = 1.2f;

    [ExportGroup("Push In")]
    [Export] public float PushInDistance { get; set; } = 0.6f;
    [Export] public float PushInSeconds { get; set; } = 0.18f;
    [Export] public float PushInHoldSeconds { get; set; } = 0.06f;
    [Export] public float PushInReturnSeconds { get; set; } = 0.22f;

    [ExportGroup("Pre-Strike")]
    [Export] public float PreStrikeDistance { get; set; } = 0.5f;
    [Export] public float PreStrikeSeconds { get; set; } = 0.12f;
    [Export] public float PreStrikeReturnSeconds { get; set; } = 0.2f;

    [ExportGroup("Whip Pan")]
    [Export] public float WhipPanDistance { get; set; } = 0.6f;
    [Export] public float WhipPanSeconds { get; set; } = 0.1f;
    [Export] public float WhipPanReturnSeconds { get; set; } = 0.16f;

    [ExportGroup("Dolly Zoom")]
    [Export] public float DollyZoomFov { get; set; } = 30.0f;
    [Export] public float DollyZoomSeconds { get; set; } = 0.6f;

    [ExportGroup("Impact Snap")]
    [Export] public float ImpactSnapFov { get; set; } = 30.0f;
    [Export] public float ImpactSnapDelay { get; set; } = 0.1f;
    [Export] public float ImpactSnapSeconds { get; set; } = 0.08f;
    [Export] public float ImpactSnapReturnSeconds { get; set; } = 0.2f;
    [Export] public float ImpactShakeIntensity { get; set; } = 0.35f;

    [ExportGroup("Rack Focus")]
    [Export] public float RackFocusWidth { get; set; } = 20.0f;
    [Export] public float RackFocusSeconds { get; set; } = 0.18f;
    [Export] public float RackFocusReturnSeconds { get; set; } = 0.28f;

    [ExportGroup("Speed Ramp")]
    [Export] public float SpeedRampMultiplier { get; set; } = 1.4f;
    [Export] public float SpeedRampSeconds { get; set; } = 0.4f;

    [ExportGroup("Shot Randomizer")]
    [Export] public bool RandomizeShots { get; set; } = false;
    [Export(PropertyHint.Range, "1,10,1")]
    public int MaxConcurrentShots { get; set; } = 2;
}
