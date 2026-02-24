using Godot;

/// <summary>
/// Next damaging hit applies a configured debuff with bonus application chance.
/// </summary>
[GlobalClass]
public partial class HexedEdgeStatusEffect : StatusEffect
{
    [Export]
    public StatusEffect DebuffToApply { get; private set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float BonusChancePercent { get; private set; } = 30f;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float BaseApplyChancePercent { get; private set; } = 100f;

    public override void OnActionInitiated(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTurnContext(context, owner)) return;
        if (!StatusRuleUtils.IsDamagingAction(context)) return;
        if (DebuffToApply == null) return;

        float chance = Mathf.Clamp(BaseApplyChancePercent + BonusChancePercent, 0f, 100f);
        context.ExtraStatusEffectsOnHit.Add(new StatusEffectChanceEntry
        {
            Effect = DebuffToApply,
            ChancePercent = chance
        });

        owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName)?.RemoveEffect(this, null);
    }
}
