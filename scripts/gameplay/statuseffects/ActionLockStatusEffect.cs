using Godot;
using System.Linq;

/// <summary>
/// Restricts which actions the owner can select/use while this status is active.
/// </summary>
[GlobalClass]
public partial class ActionLockStatusEffect : StatusEffect, IStatusActionRule
{
    [ExportGroup("Category Rules")]
    [Export]
    public Godot.Collections.Array<ActionCategory> AllowedCategories { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<ActionCategory> BlockedCategories { get; private set; } = new();

    [ExportGroup("Flags")]
    [Export]
    public bool BlockItemActions { get; private set; } = false;

    [Export]
    public bool RequirePhysicalDamagingAction { get; private set; } = false;

    [Export]
    public bool RequireMagicAction { get; private set; } = false;

    [Export]
    public string RejectReason { get; private set; } = "Action blocked by status.";

    public virtual bool IsActionAllowed(ActionData action, ItemData sourceItem, Node owner, BattleController battleController, out string reason)
    {
        reason = string.Empty;
        if (action == null)
        {
            reason = RejectReason;
            return false;
        }

        if (BlockItemActions && sourceItem != null)
        {
            reason = string.IsNullOrEmpty(RejectReason) ? "Items are disabled." : RejectReason;
            return false;
        }

        if (AllowedCategories != null && AllowedCategories.Count > 0)
        {
            bool allowed = AllowedCategories.Any(c => c == action.Category);
            if (!allowed)
            {
                reason = string.IsNullOrEmpty(RejectReason) ? "That action category is not allowed." : RejectReason;
                return false;
            }
        }

        if (BlockedCategories != null && BlockedCategories.Count > 0)
        {
            bool blocked = BlockedCategories.Any(c => c == action.Category);
            if (blocked)
            {
                reason = string.IsNullOrEmpty(RejectReason) ? "That action category is blocked." : RejectReason;
                return false;
            }
        }

        if (RequirePhysicalDamagingAction && !StatusRuleUtils.IsPhysicalDamaging(action))
        {
            reason = string.IsNullOrEmpty(RejectReason) ? "Only physical attacks are allowed." : RejectReason;
            return false;
        }

        if (RequireMagicAction && !StatusRuleUtils.IsSpellLike(action))
        {
            reason = string.IsNullOrEmpty(RejectReason) ? "Only spells are allowed." : RejectReason;
            return false;
        }

        return true;
    }
}
