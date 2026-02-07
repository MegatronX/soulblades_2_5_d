using Godot;

/// <summary>
/// Defines a noise-based camera shake pattern (e.g. Rumble, Earthquake, Impact).
/// </summary>
[GlobalClass]
public partial class CameraShakePattern : Resource
{
    [Export] public float Duration { get; set; } = 0.5f;
    [Export] public float Amplitude { get; set; } = 1.0f;
    [Export] public float Frequency { get; set; } = 15.0f;
    
    [Export] public FastNoiseLite Noise { get; set; }

    /// <summary>
    /// Optional curve to control intensity over time (0.0 to 1.0).
    /// If null, defaults to a linear fade out.
    /// </summary>
    [Export] public Curve DecayCurve { get; set; }

    public CameraShakePattern()
    {
        // Default noise settings suitable for generic shake
        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Frequency = 0.1f // Base frequency for the noise generator
        };
    }
}
