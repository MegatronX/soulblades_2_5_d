using Godot;
using System;
using System.Linq;

public static class StatusRuleUtils
{
    public static bool AreSameAction(ActionData a, ActionData b)
    {
        if (a == null || b == null) return false;
        if (ReferenceEquals(a, b)) return true;

        if (!string.IsNullOrEmpty(a.ResourcePath) && !string.IsNullOrEmpty(b.ResourcePath))
        {
            return string.Equals(a.ResourcePath, b.ResourcePath, StringComparison.Ordinal);
        }

        return string.Equals(a.CommandName, b.CommandName, StringComparison.OrdinalIgnoreCase)
            && a.Category == b.Category;
    }

    public static bool IsDamagingAction(ActionContext context)
    {
        if (context?.SourceAction == null) return false;
        if (context.SourceAction.Category == ActionCategory.Heal) return false;

        var damage = context.GetComponent<DamageComponent>();
        return damage != null && damage.Power > 0;
    }

    /// <summary>
    /// True when this context has a runtime damage component with non-zero power,
    /// meaning it should run damage/heal calculation and application.
    /// </summary>
    public static bool ShouldResolveDamage(ActionContext context)
    {
        var damage = context?.GetComponent<DamageComponent>();
        return damage != null && damage.Power != 0;
    }

    public static bool IsHealAction(ActionContext context)
    {
        if (context?.SourceAction == null) return false;
        return context.SourceAction.Category == ActionCategory.Heal;
    }

    public static bool MatchesDamageFilter(ActionData action, StatusDamageFilter filter)
    {
        if (action == null) return false;
        if (filter == StatusDamageFilter.All) return true;

        bool isPhysical = action.PhysicalRatio >= 0.5f;
        return filter switch
        {
            StatusDamageFilter.Physical => isPhysical,
            StatusDamageFilter.Magical => !isPhysical,
            _ => true
        };
    }

    public static bool IsSpellLike(ActionData action)
    {
        if (action == null) return false;
        if (action.Category == ActionCategory.Magic) return true;
        if (action.Category == ActionCategory.Heal && action.PhysicalRatio < 0.5f) return true;
        return action.PhysicalRatio < 0.5f;
    }

    public static bool IsPhysicalDamaging(ActionData action)
    {
        if (action == null) return false;
        if (action.Category == ActionCategory.Heal) return false;
        return action.PhysicalRatio >= 0.5f;
    }

    public static bool IsOwnerTurnContext(ActionContext context, Node owner)
    {
        return context != null && owner != null && context.Initiator == owner;
    }

    public static bool IsOwnerTargetContext(ActionContext context, Node owner)
    {
        return context != null && owner != null && context.CurrentTarget == owner;
    }

    public static bool IsOpponent(Node a, Node b)
    {
        if (a == null || b == null) return false;
        bool aPlayer = a.IsInGroup(GameGroups.PlayerCharacters);
        bool bPlayer = b.IsInGroup(GameGroups.PlayerCharacters);
        return aPlayer != bPlayer;
    }

    public static Node GetRandomLivingCombatant(BattleController battleController, IRandomNumberGenerator rng)
    {
        if (battleController == null) return null;
        var living = battleController.GetLivingCombatants().ToList();
        if (living.Count == 0) return null;

        int index = rng != null
            ? rng.RandRangeInt(0, living.Count - 1)
            : new RandomNumberGenerator().RandiRange(0, living.Count - 1);
        return living[index];
    }
}
