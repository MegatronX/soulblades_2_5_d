using Godot;

/// <summary>
/// Base class for camera effects that can be triggered by game events (like timed hits).
/// </summary>
[GlobalClass]
public abstract partial class CameraEffect : Resource
{
    public abstract void Apply(BattleCamera camera);
}
