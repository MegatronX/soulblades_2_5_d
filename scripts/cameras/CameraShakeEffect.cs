using Godot;

/// <summary>
/// A camera effect that applies a screen shake.
/// </summary>
[GlobalClass]
public partial class CameraShakeEffect : CameraEffect
{
    [Export] public float Intensity { get; set; } = 0.5f;

    public override void Apply(BattleCamera camera)
    {
        if (camera != null)
        {
            camera.Shake(Intensity);
        }
    }
}
