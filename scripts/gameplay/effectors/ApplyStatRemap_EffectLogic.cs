using Godot;

/// <summary>
/// Applies one or more stat remaps while the owning ability/status is active.
/// </summary>
[GlobalClass]
public partial class ApplyStatRemap_EffectLogic : EffectLogic
{
    [Export]
    public Godot.Collections.Array<StatRemapRule> Remaps { get; private set; } = new();

    public override void OnApply(Node target)
    {
        var statsComponent = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (statsComponent == null || Remaps == null) return;

        foreach (var remap in Remaps)
        {
            if (remap == null) continue;
            statsComponent.AddStatRemap(remap.SourceStat, remap.TargetStat, remap.RemapCount, this);
        }
    }

    public override void OnRemove(Node target)
    {
        var statsComponent = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (statsComponent == null) return;

        statsComponent.RemoveAllStatRemapsFromSource(this);
    }
}
