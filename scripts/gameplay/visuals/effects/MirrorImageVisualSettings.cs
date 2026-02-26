using Godot;

public readonly struct MirrorImageVisualSettings
{
    public int Count { get; }
    public float Alpha { get; }
    public float Spread { get; }
    public float DriftSpeed { get; }
    public int ForegroundGhostCount { get; }
    public float MinSpreadPixels { get; }
    public float SpreadBase { get; }
    public float SpreadScale { get; }
    public float TrailXBase { get; }
    public float TrailXScale { get; }
    public float TrailYBase { get; }
    public float TrailYScale { get; }
    public float WorldYOffsetScale { get; }
    public float DepthBase { get; }
    public float DepthBias { get; }
    public float DriftPhaseScale { get; }
    public float DriftPhaseIndexStep { get; }
    public float DriftXAmplitude { get; }
    public float DriftYAmplitude { get; }
    public float DriftZAmplitude { get; }
    public float DriftZFrequency { get; }
    public float InOutPulseAmplitude { get; }
    public float InOutPulseFrequency { get; }
    public float InOutPulseIndexStep { get; }
    public float InOutMinScale { get; }
    public float AlphaNearScale { get; }
    public float AlphaFarScale { get; }
    public float AlphaBias { get; }
    public float MinVisibleAlpha { get; }
    public Color TrailTintColor { get; }
    public float TintStrengthBase { get; }
    public float TintStrengthBySpread { get; }

    public MirrorImageVisualSettings(
        int count,
        float alpha,
        float spread,
        float driftSpeed,
        int foregroundGhostCount,
        float minSpreadPixels,
        float spreadBase,
        float spreadScale,
        float trailXBase,
        float trailXScale,
        float trailYBase,
        float trailYScale,
        float worldYOffsetScale,
        float depthBase,
        float depthBias,
        float driftPhaseScale,
        float driftPhaseIndexStep,
        float driftXAmplitude,
        float driftYAmplitude,
        float driftZAmplitude,
        float driftZFrequency,
        float inOutPulseAmplitude,
        float inOutPulseFrequency,
        float inOutPulseIndexStep,
        float inOutMinScale,
        float alphaNearScale,
        float alphaFarScale,
        float alphaBias,
        float minVisibleAlpha,
        Color trailTintColor,
        float tintStrengthBase,
        float tintStrengthBySpread)
    {
        Count = Mathf.Clamp(count, 1, 8);
        Alpha = Mathf.Clamp(alpha, 0f, 1f);
        Spread = Mathf.Max(0f, spread);
        DriftSpeed = Mathf.Max(0f, driftSpeed);
        ForegroundGhostCount = Mathf.Clamp(foregroundGhostCount, 0, 8);
        MinSpreadPixels = Mathf.Max(0f, minSpreadPixels);
        SpreadBase = Mathf.Max(0f, spreadBase);
        SpreadScale = Mathf.Max(0f, spreadScale);
        TrailXBase = Mathf.Max(0f, trailXBase);
        TrailXScale = Mathf.Max(0f, trailXScale);
        TrailYBase = Mathf.Max(0f, trailYBase);
        TrailYScale = Mathf.Max(0f, trailYScale);
        WorldYOffsetScale = Mathf.Max(0f, worldYOffsetScale);
        DepthBase = Mathf.Max(0f, depthBase);
        DepthBias = Mathf.Max(0f, depthBias);
        DriftPhaseScale = Mathf.Max(0f, driftPhaseScale);
        DriftPhaseIndexStep = Mathf.Max(0f, driftPhaseIndexStep);
        DriftXAmplitude = Mathf.Max(0f, driftXAmplitude);
        DriftYAmplitude = Mathf.Max(0f, driftYAmplitude);
        DriftZAmplitude = Mathf.Max(0f, driftZAmplitude);
        DriftZFrequency = Mathf.Max(0f, driftZFrequency);
        InOutPulseAmplitude = Mathf.Max(0f, inOutPulseAmplitude);
        InOutPulseFrequency = Mathf.Max(0f, inOutPulseFrequency);
        InOutPulseIndexStep = Mathf.Max(0f, inOutPulseIndexStep);
        InOutMinScale = Mathf.Clamp(inOutMinScale, 0f, 2f);
        AlphaNearScale = Mathf.Max(0f, alphaNearScale);
        AlphaFarScale = Mathf.Max(0f, alphaFarScale);
        AlphaBias = Mathf.Max(0f, alphaBias);
        MinVisibleAlpha = Mathf.Clamp(minVisibleAlpha, 0f, 1f);
        TrailTintColor = trailTintColor;
        TintStrengthBase = Mathf.Clamp(tintStrengthBase, 0f, 1f);
        TintStrengthBySpread = Mathf.Max(0f, tintStrengthBySpread);
    }

    public static MirrorImageVisualSettings Default => new(
        count: 1,
        alpha: 0f,
        spread: 0f,
        driftSpeed: 0f,
        foregroundGhostCount: 1,
        minSpreadPixels: 6f,
        spreadBase: 0.14f,
        spreadScale: 2.4f,
        trailXBase: 0.35f,
        trailXScale: 0.95f,
        trailYBase: 0.10f,
        trailYScale: 0.14f,
        worldYOffsetScale: 0.45f,
        depthBase: 0.006f,
        depthBias: 0.4f,
        driftPhaseScale: 0.95f,
        driftPhaseIndexStep: 0.8f,
        driftXAmplitude: 0.10f,
        driftYAmplitude: 0.055f,
        driftZAmplitude: 0.0045f,
        driftZFrequency: 0.72f,
        inOutPulseAmplitude: 0.18f,
        inOutPulseFrequency: 1.1f,
        inOutPulseIndexStep: 0.7f,
        inOutMinScale: 0.25f,
        alphaNearScale: 1.08f,
        alphaFarScale: 0.72f,
        alphaBias: 0.08f,
        minVisibleAlpha: 0.16f,
        trailTintColor: new Color(0.64f, 0.86f, 1f, 1f),
        tintStrengthBase: 0.62f,
        tintStrengthBySpread: 0.18f);

    public bool ApproxEquals(MirrorImageVisualSettings other)
    {
        return Count == other.Count
            && Mathf.Abs(Alpha - other.Alpha) <= 0.0001f
            && Mathf.Abs(Spread - other.Spread) <= 0.0001f
            && Mathf.Abs(DriftSpeed - other.DriftSpeed) <= 0.0001f
            && ForegroundGhostCount == other.ForegroundGhostCount
            && Mathf.Abs(MinSpreadPixels - other.MinSpreadPixels) <= 0.0001f
            && Mathf.Abs(SpreadBase - other.SpreadBase) <= 0.0001f
            && Mathf.Abs(SpreadScale - other.SpreadScale) <= 0.0001f
            && Mathf.Abs(TrailXBase - other.TrailXBase) <= 0.0001f
            && Mathf.Abs(TrailXScale - other.TrailXScale) <= 0.0001f
            && Mathf.Abs(TrailYBase - other.TrailYBase) <= 0.0001f
            && Mathf.Abs(TrailYScale - other.TrailYScale) <= 0.0001f
            && Mathf.Abs(WorldYOffsetScale - other.WorldYOffsetScale) <= 0.0001f
            && Mathf.Abs(DepthBase - other.DepthBase) <= 0.0001f
            && Mathf.Abs(DepthBias - other.DepthBias) <= 0.0001f
            && Mathf.Abs(DriftPhaseScale - other.DriftPhaseScale) <= 0.0001f
            && Mathf.Abs(DriftPhaseIndexStep - other.DriftPhaseIndexStep) <= 0.0001f
            && Mathf.Abs(DriftXAmplitude - other.DriftXAmplitude) <= 0.0001f
            && Mathf.Abs(DriftYAmplitude - other.DriftYAmplitude) <= 0.0001f
            && Mathf.Abs(DriftZAmplitude - other.DriftZAmplitude) <= 0.0001f
            && Mathf.Abs(DriftZFrequency - other.DriftZFrequency) <= 0.0001f
            && Mathf.Abs(InOutPulseAmplitude - other.InOutPulseAmplitude) <= 0.0001f
            && Mathf.Abs(InOutPulseFrequency - other.InOutPulseFrequency) <= 0.0001f
            && Mathf.Abs(InOutPulseIndexStep - other.InOutPulseIndexStep) <= 0.0001f
            && Mathf.Abs(InOutMinScale - other.InOutMinScale) <= 0.0001f
            && Mathf.Abs(AlphaNearScale - other.AlphaNearScale) <= 0.0001f
            && Mathf.Abs(AlphaFarScale - other.AlphaFarScale) <= 0.0001f
            && Mathf.Abs(AlphaBias - other.AlphaBias) <= 0.0001f
            && Mathf.Abs(MinVisibleAlpha - other.MinVisibleAlpha) <= 0.0001f
            && Mathf.Abs(TrailTintColor.R - other.TrailTintColor.R) <= 0.0001f
            && Mathf.Abs(TrailTintColor.G - other.TrailTintColor.G) <= 0.0001f
            && Mathf.Abs(TrailTintColor.B - other.TrailTintColor.B) <= 0.0001f
            && Mathf.Abs(TintStrengthBase - other.TintStrengthBase) <= 0.0001f
            && Mathf.Abs(TintStrengthBySpread - other.TintStrengthBySpread) <= 0.0001f;
    }
}
