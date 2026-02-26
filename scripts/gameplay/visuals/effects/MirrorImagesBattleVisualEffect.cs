using Godot;

[GlobalClass]
public partial class MirrorImagesBattleVisualEffect : BattleVisualEffect
{
    [ExportGroup("Core")]
    [Export(PropertyHint.Range, "1,8,1")]
    public int MirrorImageCount { get; private set; } = 3;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MirrorImageAlpha { get; private set; } = 0.2f;

    [Export(PropertyHint.Range, "0,0.5,0.001")]
    public float MirrorImageSpread { get; private set; } = 0.06f;

    [Export(PropertyHint.Range, "0,8,0.01")]
    public float MirrorImageDriftSpeed { get; private set; } = 1.8f;

    [ExportGroup("Layering")]
    [Export(PropertyHint.Range, "0,8,1")]
    public int ForegroundGhostCount { get; private set; } = 1;

    [ExportGroup("Placement")]
    [Export(PropertyHint.Range, "0,64,0.1")]
    public float MinSpreadPixels { get; private set; } = 6f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float SpreadBase { get; private set; } = 0.14f;

    [Export(PropertyHint.Range, "0,6,0.01")]
    public float SpreadScale { get; private set; } = 2.4f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float TrailXBase { get; private set; } = 0.35f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float TrailXScale { get; private set; } = 0.95f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float TrailYBase { get; private set; } = 0.10f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float TrailYScale { get; private set; } = 0.14f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float WorldYOffsetScale { get; private set; } = 0.45f;

    [Export(PropertyHint.Range, "0,0.05,0.0005")]
    public float DepthBase { get; private set; } = 0.006f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float DepthBias { get; private set; } = 0.4f;

    [ExportGroup("Drift")]
    [Export(PropertyHint.Range, "0,3,0.01")]
    public float DriftPhaseScale { get; private set; } = 0.95f;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float DriftPhaseIndexStep { get; private set; } = 0.8f;

    [Export(PropertyHint.Range, "0,0.5,0.001")]
    public float DriftXAmplitude { get; private set; } = 0.10f;

    [Export(PropertyHint.Range, "0,0.5,0.001")]
    public float DriftYAmplitude { get; private set; } = 0.055f;

    [Export(PropertyHint.Range, "0,0.05,0.0005")]
    public float DriftZAmplitude { get; private set; } = 0.0045f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float DriftZFrequency { get; private set; } = 0.72f;

    [ExportGroup("In/Out Pulse")]
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float InOutPulseAmplitude { get; private set; } = 0.18f;

    [Export(PropertyHint.Range, "0,6,0.01")]
    public float InOutPulseFrequency { get; private set; } = 1.1f;

    [Export(PropertyHint.Range, "0,6,0.01")]
    public float InOutPulseIndexStep { get; private set; } = 0.7f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float InOutMinScale { get; private set; } = 0.25f;

    [ExportGroup("Color & Alpha")]
    [Export(PropertyHint.Range, "0,2,0.01")]
    public float AlphaNearScale { get; private set; } = 1.08f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float AlphaFarScale { get; private set; } = 0.72f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AlphaBias { get; private set; } = 0.08f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MinVisibleAlpha { get; private set; } = 0.16f;

    [Export]
    public Color TrailTintColor { get; private set; } = new(0.64f, 0.86f, 1f, 1f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float TintStrengthBase { get; private set; } = 0.62f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float TintStrengthBySpread { get; private set; } = 0.18f;

    public override void ContributePersistent(BattleVisualStateAccumulator state, BattleVisualEffectContext context)
    {
        if (state == null || context == null) return;

        var settings = new MirrorImageVisualSettings(
            MirrorImageCount,
            MirrorImageAlpha,
            MirrorImageSpread,
            MirrorImageDriftSpeed,
            ForegroundGhostCount,
            MinSpreadPixels,
            SpreadBase,
            SpreadScale,
            TrailXBase,
            TrailXScale,
            TrailYBase,
            TrailYScale,
            WorldYOffsetScale,
            DepthBase,
            DepthBias,
            DriftPhaseScale,
            DriftPhaseIndexStep,
            DriftXAmplitude,
            DriftYAmplitude,
            DriftZAmplitude,
            DriftZFrequency,
            InOutPulseAmplitude,
            InOutPulseFrequency,
            InOutPulseIndexStep,
            InOutMinScale,
            AlphaNearScale,
            AlphaFarScale,
            AlphaBias,
            MinVisibleAlpha,
            TrailTintColor,
            TintStrengthBase,
            TintStrengthBySpread);

        state.ConsiderMirrorImages(settings, context.EffectivePriority, context.SourceOrder);
    }
}
