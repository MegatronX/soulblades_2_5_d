using Godot;

/// <summary>
/// Break acts as a defeat-state marker by forcing HP to 0 while active.
/// </summary>
[GlobalClass]
public partial class BreakStatusEffect : StatusEffect
{
    [Export]
    public bool ReviveOnRemove { get; private set; } = true;

    [Export(PropertyHint.Range, "1,100,1")]
    public int RevivePercentOnRemove { get; private set; } = 25;

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;
        if (stats.CurrentHP > 0)
        {
            stats.ModifyCurrentHP(-stats.CurrentHP);
        }
    }

    public override void OnRemove(Node owner, ActionDirector actionDirector)
    {
        base.OnRemove(owner, actionDirector);
        if (!ReviveOnRemove) return;

        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;
        if (stats.CurrentHP > 0) return;

        int maxHp = Mathf.Max(1, stats.GetStatValue(StatType.HP));
        int restore = Mathf.Clamp(Mathf.RoundToInt(maxHp * (RevivePercentOnRemove / 100f)), 1, maxHp);
        stats.ModifyCurrentHP(restore);
    }
}
