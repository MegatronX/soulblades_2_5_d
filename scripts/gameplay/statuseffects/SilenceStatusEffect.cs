using Godot;

/// <summary>
/// Silence prevents spell-like actions while active.
/// </summary>
[GlobalClass]
public partial class SilenceStatusEffect : StatusEffect, IStatusActionRule
{
    [Export]
    public bool BlockItemActions { get; private set; } = false;

    [Export]
    public string RejectReason { get; private set; } = "Silenced.";

    public bool IsActionAllowed(ActionData action, ItemData sourceItem, Node owner, BattleController battleController, out string reason)
    {
        reason = string.Empty;

        if (action == null)
        {
            reason = string.IsNullOrEmpty(RejectReason) ? "Action blocked." : RejectReason;
            return false;
        }

        if (BlockItemActions && sourceItem != null)
        {
            reason = string.IsNullOrEmpty(RejectReason) ? "Items are blocked." : RejectReason;
            return false;
        }

        if (StatusRuleUtils.IsSpellLike(action))
        {
            reason = string.IsNullOrEmpty(RejectReason) ? "Spellcasting is blocked." : RejectReason;
            return false;
        }

        return true;
    }
}
