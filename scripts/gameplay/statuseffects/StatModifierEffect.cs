using Godot;

/// <summary>
/// A data-driven status effect that applies a modifier to a specific stat.
/// Can be used for Haste, Defend, Attack Up, etc.
/// </summary>
[GlobalClass]
public partial class StatModifierEffect : StatusEffect
{
    [Export]
    public Godot.Collections.Array<StatMultiplierEntry> StatMultipliers { get; private set; } = new();

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);

        var stats = owner.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        if (StatMultipliers == null || StatMultipliers.Count == 0)
        {
            GD.PrintErr($"{EffectName}: No stat multipliers configured.");
            return;
        }

        var appliedDescriptions = new System.Collections.Generic.List<string>();
        foreach (var entry in StatMultipliers)
        {
            if (entry == null) continue;
            var effectiveMultiplier = entry.Multiplier;
            if (effectiveMultiplier <= 0f)
            {
                GD.PrintErr($"{EffectName}: Multiplier for {entry.Stat} must be > 0. Defaulting to 1.0.");
                effectiveMultiplier = 1.0f;
            }

            if (effectiveMultiplier != 1.0f)
            {
                var multModifier = new StatModifier(entry.Stat, effectiveMultiplier, ModifierType.Multiplicative, this);
                stats.AddModifier(multModifier);
            }
            appliedDescriptions.Add($"{entry.Stat} x{effectiveMultiplier}");
        }

        if (appliedDescriptions.Count > 0)
        {
            GD.Print($"{owner.Name} is now affected by {EffectName}! ({string.Join(", ", appliedDescriptions)})");
        }
    }

    public override void OnRemove(Node owner, ActionDirector actionDirector)
    {
        base.OnRemove(owner, actionDirector);

        var stats = owner.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        stats.RemoveAllModifiersFromSource(this);
        GD.Print($"{EffectName} has worn off for {owner.Name}.");
    }
}
