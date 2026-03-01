using Godot;

/// <summary>
/// Weighted spawn entry for ambient props.
/// Enables per-prop frequency tuning and optional per-prop active limits.
/// </summary>
[GlobalClass]
public partial class AmbientPropSpawnEntry : Resource
{
    [ExportCategory("Ambient Prop Spawn Entry")]
    [Export]
    public PackedScene PropScene { get; private set; }

    [ExportGroup("Frequency and Caps")]
    [Export(PropertyHint.Range, "1,1000,1,suffix:weight")]
    public int Weight { get; private set; } = 10;

    [Export(PropertyHint.Range, "0,60,0.1,suffix:s")]
    public float SpawnCooldownSeconds { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,256,1,suffix:props")]
    public int MaxActiveCount { get; private set; } = 0;

    [ExportGroup("Optional Motion and Lifetime")]
    [Export]
    public bool EnableSystemMotion { get; private set; } = false;

    [Export(PropertyHint.Range, "0,5,0.01,suffix:m")]
    public float MotionRadius { get; private set; } = 0.35f;

    [Export(PropertyHint.Range, "0.01,8,0.01,suffix:hz")]
    public float MotionSpeedMin { get; private set; } = 0.3f;

    [Export(PropertyHint.Range, "0.01,8,0.01,suffix:hz")]
    public float MotionSpeedMax { get; private set; } = 0.8f;

    [Export(PropertyHint.Range, "0,3,0.01,suffix:m")]
    public float VerticalBobAmplitude { get; private set; } = 0.12f;

    [Export(PropertyHint.Range, "0,180,0.1,suffix:s")]
    public float LifetimeMinSeconds { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,180,0.1,suffix:s")]
    public float LifetimeMaxSeconds { get; private set; } = 0f;
}
