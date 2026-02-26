using Godot;

[GlobalClass]
public partial class ForceInjuredIdleBattleVisualEffect : BattleVisualEffect
{
    public override void ContributePersistent(BattleVisualStateAccumulator state, BattleVisualEffectContext context)
    {
        if (state == null) return;
        state.SetForceInjuredIdle();
    }
}
