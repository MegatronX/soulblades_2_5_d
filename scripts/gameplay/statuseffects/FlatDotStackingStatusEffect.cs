using Godot;

/// <summary>
/// End-of-turn flat damage/heal with stack scaling.
/// </summary>
[GlobalClass]
public partial class FlatDotStackingStatusEffect : StackingStatusEffect
{
    public enum TickCombineMode
    {
        FlatOnly = 0,
        PercentOnly = 1,
        MaxFlatOrPercent = 2
    }

    [ExportGroup("Flat Tick")]
    [Export]
    public int BaseTickNormal { get; private set; } = 10;

    [Export]
    public int BaseTickBoss { get; private set; } = -1;

    [Export]
    public int TickPerAdditionalStackNormal { get; private set; } = 0;

    [Export]
    public int TickPerAdditionalStackBoss { get; private set; } = -1;

    [ExportGroup("Percent Tick")]
    [Export]
    public TickCombineMode PercentTickMode { get; private set; } = TickCombineMode.FlatOnly;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float BasePercentNormal { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float BasePercentBoss { get; private set; } = -1f;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float PerStackPercentNormal { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,1,0.0001")]
    public float PerStackPercentBoss { get; private set; } = -1f;

    [ExportGroup("Behavior")]
    [Export]
    public bool HealInsteadOfDamage { get; private set; } = false;

    public override void OnTurnEnd(Node owner, ActionDirector actionDirector)
    {
        base.OnTurnEnd(owner, actionDirector);
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (stats == null || manager == null) return;

        var instance = manager.GetEffectInstance(this);
        if (instance == null) return;
        int stacks = Mathf.Max(1, instance.Stacks);

        bool isBoss = owner != null && !owner.IsInGroup(GameGroups.PlayerCharacters);
        int totalTick = ResolveTickAmount(stats, isBoss, stacks);
        totalTick = Mathf.Max(0, totalTick);
        if (totalTick <= 0) return;

        stats.ModifyCurrentHP(HealInsteadOfDamage ? totalTick : -totalTick);
    }

    protected virtual int ResolveTickAmount(StatsComponent stats, bool isBoss, int stacks)
    {
        int baseTick = isBoss && BaseTickBoss >= 0 ? BaseTickBoss : BaseTickNormal;
        int perStack = isBoss && TickPerAdditionalStackBoss >= 0 ? TickPerAdditionalStackBoss : TickPerAdditionalStackNormal;
        int flatTick = Mathf.Max(0, baseTick + ((Mathf.Max(1, stacks) - 1) * perStack));

        int percentTick = 0;
        if (PercentTickMode != TickCombineMode.FlatOnly)
        {
            float basePct = isBoss && BasePercentBoss >= 0f ? BasePercentBoss : BasePercentNormal;
            float perStackPct = isBoss && PerStackPercentBoss >= 0f ? PerStackPercentBoss : PerStackPercentNormal;
            float totalPct = Mathf.Max(0f, basePct + ((Mathf.Max(1, stacks) - 1) * perStackPct));
            percentTick = Mathf.Max(0, Mathf.RoundToInt(stats.GetStatValue(StatType.HP) * totalPct));
        }

        return PercentTickMode switch
        {
            TickCombineMode.PercentOnly => percentTick,
            TickCombineMode.MaxFlatOrPercent => Mathf.Max(flatTick, percentTick),
            _ => flatTick
        };
    }
}
