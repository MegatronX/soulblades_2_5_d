using Godot;

public readonly struct HoverVisualSettings
{
    public float BaseLiftPixels { get; }
    public float BobAmplitudePixels { get; }
    public float BobSpeed { get; }
    public float PhaseOffsetPerSprite { get; }
    public HoverBobWaveform BobWaveform { get; }

    public HoverVisualSettings(
        float baseLiftPixels,
        float bobAmplitudePixels,
        float bobSpeed,
        float phaseOffsetPerSprite,
        HoverBobWaveform bobWaveform)
    {
        BaseLiftPixels = Mathf.Max(0f, baseLiftPixels);
        BobAmplitudePixels = Mathf.Max(0f, bobAmplitudePixels);
        BobSpeed = Mathf.Max(0f, bobSpeed);
        PhaseOffsetPerSprite = Mathf.Max(0f, phaseOffsetPerSprite);
        BobWaveform = bobWaveform;
    }

    public static HoverVisualSettings Default => new(0f, 0f, 0f, 0f, HoverBobWaveform.Sine);

    public bool IsEnabled => BaseLiftPixels > 0f || BobAmplitudePixels > 0f;

    public bool ApproxEquals(HoverVisualSettings other)
    {
        return Mathf.Abs(BaseLiftPixels - other.BaseLiftPixels) <= 0.0001f
            && Mathf.Abs(BobAmplitudePixels - other.BobAmplitudePixels) <= 0.0001f
            && Mathf.Abs(BobSpeed - other.BobSpeed) <= 0.0001f
            && Mathf.Abs(PhaseOffsetPerSprite - other.PhaseOffsetPerSprite) <= 0.0001f
            && BobWaveform == other.BobWaveform;
    }
}
