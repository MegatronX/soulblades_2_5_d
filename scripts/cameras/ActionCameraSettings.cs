using Godot;

[GlobalClass]
public partial class ActionCameraSettings : Resource
{
    [Export]
    public CameraProfile.CameraShotOptions AllowedShots { get; set; } = CameraProfile.CameraShotOptions.None;
}
