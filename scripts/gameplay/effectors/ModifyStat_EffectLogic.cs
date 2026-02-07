using Godot;

/// <summary>
/// An EffectLogic that applies a stat modification to the target.
/// </summary>
[GlobalClass]
public partial class ModifyStat_EffectLogic : EffectLogic
{
    [Export]
    public StatType StatToModify { get; private set; }

    [Export]
    public ModifierType ModificationType { get; private set; }

    [Export]
    public float Value { get; private set; }

    public override void OnApply(Node target)
    {
        var statsComponent = target.GetNode<StatsComponent>(StatsComponent.NodeName);
        if (statsComponent == null) return;

        // 'this' is the source of the modifier, ensuring we can remove it later.
        var modifier = new StatModifier(StatToModify, Value, ModificationType, this);
        statsComponent.AddModifier(modifier);
    }

    public override void OnRemove(Node target)
    {
        var statsComponent = target.GetNode<StatsComponent>(StatsComponent.NodeName);
        if (statsComponent == null) return;

        statsComponent.RemoveAllModifiersFromSource(this);
    }
}