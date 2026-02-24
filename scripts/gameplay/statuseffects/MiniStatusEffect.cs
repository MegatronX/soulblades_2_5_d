using Godot;

/// <summary>
/// Mini grants high evasiveness but weakens outgoing damage.
/// </summary>
[GlobalClass]
public partial class MiniStatusEffect : StatusEffect
{
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float EvasionBonusPercent { get; private set; } = 0.50f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float PhysicalDamageDealtMultiplier { get; private set; } = 0.50f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float MagicalDamageDealtMultiplier { get; private set; } = 0.75f;

    public override void OnActionInitiated(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTurnContext(context, owner)) return;
        if (!StatusRuleUtils.IsDamagingAction(context)) return;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null) return;

        float mult = StatusRuleUtils.MatchesDamageFilter(context.SourceAction, StatusDamageFilter.Physical)
            ? PhysicalDamageDealtMultiplier
            : MagicalDamageDealtMultiplier;

        damage.Power = Mathf.Max(0, Mathf.RoundToInt(damage.Power * Mathf.Max(0f, mult)));
    }

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;
        if (!StatusRuleUtils.IsDamagingAction(context)) return;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null) return;

        float keep = Mathf.Clamp(1.0f - EvasionBonusPercent, 0.05f, 1.0f);
        damage.Accuracy = Mathf.Clamp(Mathf.RoundToInt(damage.Accuracy * keep), 1, 100);
    }
}
