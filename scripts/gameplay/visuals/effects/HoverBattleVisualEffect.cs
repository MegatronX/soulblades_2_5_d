using Godot;

[GlobalClass]
public partial class HoverBattleVisualEffect : BattleVisualEffect
{
    [ExportGroup("Hover")]
    [Export(PropertyHint.Range, "0,128,0.1")]
    public float BaseLiftPixels { get; private set; } = 8f;

    [Export(PropertyHint.Range, "0,64,0.1")]
    public float BobAmplitudePixels { get; private set; } = 2.5f;

    [Export(PropertyHint.Range, "0,16,0.01")]
    public float BobSpeed { get; private set; } = 2.2f;

    [Export(PropertyHint.Range, "0,6.28319,0.01")]
    public float PhaseOffsetPerSprite { get; private set; } = 0.45f;

    [Export]
    public HoverBobWaveform BobWaveform { get; private set; } = HoverBobWaveform.Sine;

    public override void ContributePersistent(BattleVisualStateAccumulator state, BattleVisualEffectContext context)
    {
        if (state == null || context == null) return;

        var settings = new HoverVisualSettings(
            BaseLiftPixels,
            BobAmplitudePixels,
            BobSpeed,
            PhaseOffsetPerSprite,
            BobWaveform);

        state.ConsiderHover(settings, context.EffectivePriority, context.SourceOrder);
    }
}
