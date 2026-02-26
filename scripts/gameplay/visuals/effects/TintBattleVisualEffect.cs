using Godot;

[GlobalClass]
public partial class TintBattleVisualEffect : BattleVisualEffect
{
    [Export]
    public Color TintColor { get; private set; } = Colors.White;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float TintStrength { get; private set; } = 0.35f;

    public override void ContributePersistent(BattleVisualStateAccumulator state, BattleVisualEffectContext context)
    {
        if (state == null || context == null) return;
        state.ConsiderTint(TintColor, TintStrength, context.EffectivePriority, context.SourceOrder);
    }
}
