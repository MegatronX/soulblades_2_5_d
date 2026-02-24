using Godot;
using System.Linq;

/// <summary>
/// Toad applies heavy stat penalties and restricts available actions.
/// </summary>
[GlobalClass]
public partial class ToadStatusEffect : StatusEffect, IStatusActionRule
{
    [Export(PropertyHint.Range, "0,3,0.01")]
    public float AttackMultiplier { get; private set; } = 0.40f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float MagicMultiplier { get; private set; } = 0.40f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float DefenseMultiplier { get; private set; } = 0.80f;

    [Export]
    public bool AllowItems { get; private set; } = true;

    [Export]
    public Godot.Collections.Array<ActionCategory> AllowedCategories { get; private set; } = new() { ActionCategory.Attack };

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        if (AttackMultiplier != 1.0f)
        {
            stats.AddModifier(new StatModifier(StatType.Strength, AttackMultiplier, ModifierType.Multiplicative, this));
        }
        if (MagicMultiplier != 1.0f)
        {
            stats.AddModifier(new StatModifier(StatType.Magic, MagicMultiplier, ModifierType.Multiplicative, this));
        }
        if (DefenseMultiplier != 1.0f)
        {
            stats.AddModifier(new StatModifier(StatType.Defense, DefenseMultiplier, ModifierType.Multiplicative, this));
        }
    }

    public override void OnRemove(Node owner, ActionDirector actionDirector)
    {
        base.OnRemove(owner, actionDirector);
        owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName)?.RemoveAllModifiersFromSource(this);
    }

    public bool IsActionAllowed(ActionData action, ItemData sourceItem, Node owner, BattleController battleController, out string reason)
    {
        reason = string.Empty;
        if (action == null)
        {
            reason = "Toad prevents this action.";
            return false;
        }

        if (sourceItem != null)
        {
            if (AllowItems) return true;
            reason = "Toad prevents item usage.";
            return false;
        }

        if (AllowedCategories == null || AllowedCategories.Count == 0)
        {
            reason = "Toad prevents this action.";
            return false;
        }

        if (!AllowedCategories.Any(c => c == action.Category))
        {
            reason = "Toad allows only basic actions.";
            return false;
        }

        return true;
    }
}
