using Godot;

/// <summary>
/// Bleed deals flat DoT, grants bonus crit chance against the target, and
/// triggers one extra flat damage proc on the first physical hit each turn.
/// </summary>
[GlobalClass]
public partial class BleedStatusEffect : FlatDotStackingStatusEffect
{
    [Export]
    public int MaxBleedStacks { get; private set; } = 5;

    [Export]
    public int BaseBonusProcNormal { get; private set; } = 10;

    [Export]
    public int BaseBonusProcBoss { get; private set; } = 6;

    [Export]
    public int CritBonusPerStackPercent { get; private set; } = 2;

    [Export]
    public int CritBonusCapPercent { get; private set; } = 10;

    public override int ResolveMaxStacks(Node owner) => Mathf.Max(1, MaxBleedStacks);

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName)?.SetState(this, "bleed_bonus_used", false);
    }

    public override void OnTurnStart(Node owner, ActionDirector actionDirector)
    {
        base.OnTurnStart(owner, actionDirector);
        owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName)?.SetState(this, "bleed_bonus_used", false);
    }

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;
        if (context?.Initiator == null || !StatusRuleUtils.IsOpponent(owner, context.Initiator)) return;
        if (context.SourceAction == null) return;

        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        var instance = manager?.GetEffectInstance(this);
        if (instance == null) return;

        int critBonus = Mathf.Clamp(instance.Stacks * CritBonusPerStackPercent, 0, CritBonusCapPercent);
        context.BonusCritChancePercent += critBonus;
    }

    public override void OnActionPostExecution(ActionContext context, Node owner, ActionResult result)
    {
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;
        if (result == null || !result.IsHit || result.FinalDamage <= 0) return;
        if (!StatusRuleUtils.MatchesDamageFilter(context.SourceAction, StatusDamageFilter.Physical)) return;

        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        var instance = manager?.GetEffectInstance(this);
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (manager == null || instance == null || stats == null) return;

        if (manager.TryGetState(this, "bleed_bonus_used", out var used) && used.AsBool())
        {
            return;
        }

        bool isBoss = owner != null && !owner.IsInGroup(GameGroups.PlayerCharacters);
        int baseBonus = isBoss ? BaseBonusProcBoss : BaseBonusProcNormal;
        int bonus = Mathf.Max(0, baseBonus * Mathf.Max(1, instance.Stacks));
        if (bonus <= 0) return;

        stats.ModifyCurrentHP(-bonus);
        manager.SetState(this, "bleed_bonus_used", true);
    }
}
