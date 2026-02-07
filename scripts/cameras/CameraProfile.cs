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
        ParallaxSweep = 1 << 2
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
}
