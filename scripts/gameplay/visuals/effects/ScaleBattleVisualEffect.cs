using Godot;

[GlobalClass]
public partial class ScaleBattleVisualEffect : BattleVisualEffect
{
    [Export(PropertyHint.Range, "0.1,4,0.01")]
    public float ScaleMultiplier { get; private set; } = 1.0f;

    public override void ContributePersistent(BattleVisualStateAccumulator state, BattleVisualEffectContext context)
    {
        if (state == null) return;
        state.MultiplyScale(ScaleMultiplier);
    }
}
