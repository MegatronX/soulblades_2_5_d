using Godot;

/// <summary>
/// Berserk boosts offense/tempo, increases vulnerability, and restricts action options.
/// </summary>
[GlobalClass]
public partial class BerserkStatusEffect : AIOverrideStatusEffect, IStatusActionRule
{
    [Export(PropertyHint.Range, "0,3,0.01")]
    public float AttackMultiplier { get; private set; } = 1.33f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float SpeedMultiplier { get; private set; } = 1.20f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float DefenseMultiplier { get; private set; } = 0.85f;

    [Export]
    public bool BlockMagicActions { get; private set; } = true;

    [Export]
    public bool BlockItemActions { get; private set; } = true;

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        if (AttackMultiplier != 1.0f)
        {
            stats.AddModifier(new StatModifier(StatType.Strength, AttackMultiplier, ModifierType.Multiplicative, this));
        }
        if (SpeedMultiplier != 1.0f)
        {
            stats.AddModifier(new StatModifier(StatType.Speed, SpeedMultiplier, ModifierType.Multiplicative, this));
        }
        if (DefenseMultiplier != 1.0f)
        {
            stats.AddModifier(new StatModifier(StatType.Defense, DefenseMultiplier, ModifierType.Multiplicative, this));
            stats.AddModifier(new StatModifier(StatType.MagicDefense, DefenseMultiplier, ModifierType.Multiplicative, this));
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
            reason = "Berserk prevents this action.";
            return false;
        }

        if (BlockItemActions && sourceItem != null)
        {
            reason = "Berserk prevents item usage.";
            return false;
        }

        if (BlockMagicActions && StatusRuleUtils.IsSpellLike(action))
        {
            reason = "Berserk prevents spell usage.";
            return false;
        }

        if (!StatusRuleUtils.IsPhysicalDamaging(action))
        {
            reason = "Berserk only allows physical attacks.";
            return false;
        }

        return true;
    }
}
