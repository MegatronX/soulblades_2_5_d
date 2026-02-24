using Godot;

/// <summary>
/// End-of-turn damage/heal based on % max HP with stack-aware scaling.
/// </summary>
[GlobalClass]
public partial class PercentDotStackingStatusEffect : StackingStatusEffect
{
    [ExportGroup("Percent Tick")]
    [Export(PropertyHint.Range, "0,1,0.001")]
    public float BasePercentNormal { get; private set; } = 0.03f;

    [Export(PropertyHint.Range, "0,1,0.001")]
    public float BasePercentBoss { get; private set; } = -1f;

    [Export(PropertyHint.Range, "0,1,0.001")]
    public float PerStackPercentNormal { get; private set; } = 0.0f;

    [Export(PropertyHint.Range, "0,1,0.001")]
    public float PerStackPercentBoss { get; private set; } = -1f;

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
        float basePct = isBoss && BasePercentBoss >= 0f ? BasePercentBoss : BasePercentNormal;
        float perStack = isBoss && PerStackPercentBoss >= 0f ? PerStackPercentBoss : PerStackPercentNormal;
        float totalPct = Mathf.Max(0f, basePct + ((stacks - 1) * perStack));

        int maxHp = stats.GetStatValue(StatType.HP);
        int amount = Mathf.RoundToInt(maxHp * totalPct);
        if (amount <= 0) return;

        stats.ModifyCurrentHP(HealInsteadOfDamage ? amount : -amount);
    }
}
