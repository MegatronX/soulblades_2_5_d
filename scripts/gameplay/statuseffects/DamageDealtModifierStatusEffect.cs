using Godot;

/// <summary>
/// Modifies outgoing damage/healing by the owner with optional one-shot consumption.
/// </summary>
[GlobalClass]
public partial class DamageDealtModifierStatusEffect : StatusEffect
{
    [Export]
    public StatusDamageFilter Filter { get; private set; } = StatusDamageFilter.All;

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float OutgoingMultiplier { get; private set; } = 1.0f;

    [Export]
    public bool RequireDamagingAction { get; private set; } = true;

    [Export]
    public bool ConsumeOnTrigger { get; private set; } = false;

    public override void OnActionInitiated(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTurnContext(context, owner)) return;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null) return;
        if (!StatusRuleUtils.MatchesDamageFilter(context.SourceAction, Filter)) return;
        if (RequireDamagingAction && !StatusRuleUtils.IsDamagingAction(context)) return;

        if (OutgoingMultiplier != 1.0f)
        {
            damage.Power = Mathf.Max(0, Mathf.RoundToInt(damage.Power * OutgoingMultiplier));
        }

        if (!ConsumeOnTrigger) return;

        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        manager?.RemoveEffect(this, null);
    }
}
