using Godot;

/// <summary>
/// Modifies incoming damage (and optionally healing) on the owner.
/// Supports one-shot consumption and next-turn fallback expiry.
/// </summary>
[GlobalClass]
public partial class DamageTakenModifierStatusEffect : StatusEffect
{
    [ExportGroup("Damage")]
    [Export]
    public StatusDamageFilter Filter { get; private set; } = StatusDamageFilter.All;

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float DamageTakenMultiplier { get; private set; } = 1.0f;

    [ExportGroup("Healing")]
    [Export]
    public bool ModifyIncomingHealing { get; private set; } = false;

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float HealingReceivedMultiplier { get; private set; } = 1.0f;

    [ExportGroup("Consumption")]
    [Export]
    public bool ConsumeOnFirstDamageTrigger { get; private set; } = false;

    [Export]
    public bool ExpireAtOwnerTurnStartIfUnused { get; private set; } = false;

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        manager?.SetState(this, "consumed", false);
    }

    public override void OnTurnStart(Node owner, ActionDirector actionDirector)
    {
        base.OnTurnStart(owner, actionDirector);
        if (!ExpireAtOwnerTurnStartIfUnused) return;

        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (manager == null) return;
        if (manager.TryGetState(this, "consumed", out var consumed) && consumed.AsBool()) return;

        manager.RemoveEffect(this, actionDirector);
    }

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null) return;

        bool consumed = false;
        bool isHeal = StatusRuleUtils.IsHealAction(context);

        if (isHeal)
        {
            if (ModifyIncomingHealing && HealingReceivedMultiplier != 1.0f && damage.Power > 0)
            {
                damage.Power = Mathf.Max(0, Mathf.RoundToInt(damage.Power * HealingReceivedMultiplier));
            }
            return;
        }

        if (!StatusRuleUtils.IsDamagingAction(context)) return;
        if (!StatusRuleUtils.MatchesDamageFilter(context.SourceAction, Filter)) return;

        if (DamageTakenMultiplier != 1.0f)
        {
            damage.Power = Mathf.Max(0, Mathf.RoundToInt(damage.Power * DamageTakenMultiplier));
        }
        consumed = ConsumeOnFirstDamageTrigger;

        if (consumed)
        {
            var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
            if (manager == null) return;
            manager.SetState(this, "consumed", true);
            manager.RemoveEffect(this, null);
        }
    }
}
