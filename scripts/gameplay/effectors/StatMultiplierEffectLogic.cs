using Godot;

/// <summary>
/// Applies one or more multiplicative stat modifiers to the target.
/// </summary>
[GlobalClass]
public partial class StatMultiplierEffectLogic : EffectLogic, ITurnPreviewStatDeltaProvider
{
    [Export]
    public Godot.Collections.Array<StatMultiplierEntry> StatMultipliers { get; private set; } = new();

    public override void OnApply(Node target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        if (StatMultipliers == null || StatMultipliers.Count == 0) return;

        foreach (var entry in StatMultipliers)
        {
            if (entry == null) continue;
            var multiplier = entry.Multiplier;
            if (multiplier <= 0f)
            {
                GD.PrintErr($"StatMultiplierEffectLogic: Multiplier for {entry.Stat} must be > 0. Defaulting to 1.0.");
                multiplier = 1.0f;
            }

            if (multiplier == 1.0f) continue;

            var modifier = new StatModifier(entry.Stat, multiplier, ModifierType.Multiplicative, this);
            stats.AddModifier(modifier);
        }
    }

    public override void OnRemove(Node target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        stats.RemoveAllModifiersFromSource(this);
    }

    public System.Collections.Generic.IEnumerable<TurnPreviewStatDelta> GetTurnPreviewStatDeltas()
    {
        if (StatMultipliers == null) yield break;

        foreach (var entry in StatMultipliers)
        {
            if (entry == null) continue;
            yield return new TurnPreviewStatDelta(entry.Stat, 0, entry.Multiplier);
        }
    }
}
