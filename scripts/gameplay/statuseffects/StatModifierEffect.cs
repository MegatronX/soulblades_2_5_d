using Godot;

/// <summary>
/// A data-driven status effect that applies a modifier to a specific stat.
/// Can be used for Haste, Defend, Attack Up, etc.
/// </summary>
[GlobalClass]
public partial class StatModifierEffect : StatusEffect
{
    [Export]
    public StatType StatToModify { get; private set; }

    [Export]
    public float Multiplier { get; private set; } = 1.5f;

    // We would need a StatsComponent on the character that can handle temporary modifiers.
    // For example: owner.GetNode<StatsComponent>("StatsComponent").AddModifier(this);

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        // In a real implementation, you would get the character's StatsComponent
        // and tell it to add a modifier associated with this effect.
        GD.Print($"{owner.Name} is now affected by {EffectName}! ({StatToModify} x{Multiplier})");
    }

    public override void OnRemove(Node owner, ActionDirector actionDirector)
    {
        // Similarly, you would tell the StatsComponent to remove the modifier
        // associated with this effect.
        GD.Print($"{EffectName} has worn off for {owner.Name}.");
    }
}