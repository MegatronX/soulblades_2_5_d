using Godot;

[GlobalClass]
public partial class ShaderBattleVisualEffect : BattleVisualEffect
{
    [Export]
    public ShaderMaterial ShaderMaterial { get; private set; }

    public override void ContributePersistent(BattleVisualStateAccumulator state, BattleVisualEffectContext context)
    {
        if (state == null || context == null) return;
        if (ShaderMaterial == null) return;
        state.ConsiderShader(ShaderMaterial, context.EffectivePriority, context.SourceOrder);
    }
}
