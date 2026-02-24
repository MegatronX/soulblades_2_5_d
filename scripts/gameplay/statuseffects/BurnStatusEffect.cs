using Godot;

/// <summary>
/// Burn applies end-of-turn flat damage and reduces incoming healing.
/// Optional scorch-overcap behavior is included for build-up loops.
/// </summary>
[GlobalClass]
public partial class BurnStatusEffect : StackingStatusEffect
{
    [ExportGroup("Flat Tick")]
    [Export]
    public int BaseTickNormal { get; private set; } = 16;

    [Export]
    public int BaseTickBoss { get; private set; } = 8;

    [Export]
    public int MaxStacksOverride { get; private set; } = 5;

    [ExportGroup("Percent Tick")]
    [Export]
    public FlatDotStackingStatusEffect.TickCombineMode PercentTickMode { get; private set; } = FlatDotStackingStatusEffect.TickCombineMode.FlatOnly;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float BasePercentNormal { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float BasePercentBoss { get; private set; } = -1f;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float PerStackPercentNormal { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float PerStackPercentBoss { get; private set; } = -1f;

    [ExportGroup("Healing Penalty")]
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BaseHealingPenalty { get; private set; } = 0.25f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AdditionalPenaltyPerStack { get; private set; } = 0.05f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MinHealingMultiplier { get; private set; } = 0.10f;

    [ExportGroup("Scorch Pop")]
    [Export]
    public bool EnableScorchPop { get; private set; } = false;

    [Export]
    public int ScorchResetStacks { get; private set; } = 3;

    [Export(PropertyHint.Range, "0,3,0.05")]
    public float ScorchExtraTickMultiplier { get; private set; } = 1.0f;

    public override int ResolveMaxStacks(Node owner)
    {
        int inherited = base.ResolveMaxStacks(owner);
        if (MaxStacksOverride > 0) return Mathf.Min(inherited, MaxStacksOverride);
        return inherited;
    }

    public override bool OnReapply(StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, ActionDirector actionDirector, IRandomNumberGenerator rng = null)
    {
        bool changed = false;
        int maxStacks = ResolveMaxStacks(manager?.Owner);
        int previous = instance.Stacks;
        int proposed = previous + 1;

        if (EnableScorchPop && previous >= maxStacks)
        {
            ApplyTick(manager?.Owner, instance, Mathf.Max(0.0f, ScorchExtraTickMultiplier));
            instance.Stacks = Mathf.Clamp(ScorchResetStacks, 1, maxStacks);
            changed = true;
        }
        else
        {
            instance.Stacks = Mathf.Clamp(proposed, 1, maxStacks);
            changed = changed || instance.Stacks != previous;
        }

        if (RefreshDurationOnReapply)
        {
            int oldTurns = instance.RemainingTurns;
            instance.RemainingTurns = manager.RollDuration(this, rng);
            changed = changed || oldTurns != instance.RemainingTurns;
        }

        OnStacksChanged(manager?.Owner, manager, instance, actionDirector);
        return changed;
    }

    public override void OnTurnEnd(Node owner, ActionDirector actionDirector)
    {
        base.OnTurnEnd(owner, actionDirector);
        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        var instance = manager?.GetEffectInstance(this);
        if (instance == null) return;

        ApplyTick(owner, instance, 1.0f);
    }

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;
        if (!StatusRuleUtils.IsHealAction(context)) return;

        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        var instance = manager?.GetEffectInstance(this);
        var damage = context.GetComponent<DamageComponent>();
        if (instance == null || damage == null || damage.Power <= 0) return;

        float penalty = BaseHealingPenalty + ((Mathf.Max(1, instance.Stacks) - 1) * AdditionalPenaltyPerStack);
        float multiplier = Mathf.Clamp(1.0f - penalty, MinHealingMultiplier, 1.0f);
        damage.Power = Mathf.Max(0, Mathf.RoundToInt(damage.Power * multiplier));
    }

    private void ApplyTick(Node owner, StatusEffectManager.StatusEffectInstance instance, float multiplier)
    {
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null || instance == null) return;

        bool isBoss = owner != null && !owner.IsInGroup(GameGroups.PlayerCharacters);
        int stacks = Mathf.Max(1, instance.Stacks);
        int tick = ResolveTickAmount(stats, isBoss, stacks);
        tick = Mathf.RoundToInt(tick * Mathf.Max(0f, multiplier));
        if (tick <= 0) return;

        stats.ModifyCurrentHP(-tick);
    }

    private int ResolveTickAmount(StatsComponent stats, bool isBoss, int stacks)
    {
        int flatBase = isBoss ? BaseTickBoss : BaseTickNormal;
        int flatTick = Mathf.Max(0, flatBase * Mathf.Max(1, stacks));

        int percentTick = 0;
        if (PercentTickMode != FlatDotStackingStatusEffect.TickCombineMode.FlatOnly)
        {
            float basePct = isBoss && BasePercentBoss >= 0f ? BasePercentBoss : BasePercentNormal;
            float perStackPct = isBoss && PerStackPercentBoss >= 0f ? PerStackPercentBoss : PerStackPercentNormal;
            float totalPct = Mathf.Max(0f, basePct + ((Mathf.Max(1, stacks) - 1) * perStackPct));
            percentTick = Mathf.Max(0, Mathf.RoundToInt(stats.GetStatValue(StatType.HP) * totalPct));
        }

        return PercentTickMode switch
        {
            FlatDotStackingStatusEffect.TickCombineMode.PercentOnly => percentTick,
            FlatDotStackingStatusEffect.TickCombineMode.MaxFlatOrPercent => Mathf.Max(flatTick, percentTick),
            _ => flatTick
        };
    }
}
