using Godot;

/// <summary>
/// Defines a music track for battle, including volume settings and selection weight.
/// </summary>
[GlobalClass]
public partial class BattleMusicData : Resource
{
    [Export]
    public AudioStream Stream { get; private set; }

    [Export]
    public AudioStream LowHealthLayer { get; private set; }

    [Export(PropertyHint.Range, "1,100,1")]
    public int Weight { get; private set; } = 10;

    [Export(PropertyHint.Range, "-80,24,0.1")]
    public float VolumeDb { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0.1,4.0,0.01")]
    public float PitchScale { get; private set; } = 1.0f;

    [Export]
    public bool Loop { get; private set; } = true;
}