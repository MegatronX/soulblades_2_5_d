using Godot;
using System;
using System.Linq;

/// <summary>
/// Berserk variant that applies stat buffs and forces auto basic attacks via AI override.
/// </summary>
[GlobalClass]
public partial class BerserkAutoAttackStatusEffect : BerserkStatusEffect, IStatusActionRule
{
    [Export(PropertyHint.ResourceType, "ActionData")]
    public ActionData BasicAttackAction { get; private set; }

    [Export]
    public bool RandomTargetSelection { get; private set; } = false;

    [Export]
    public string ForcedAttackRejectReason { get; private set; } = "Frenzy forces a basic attack.";

    protected override bool ShouldApplyAiOverride(Node owner)
    {
        return true;
    }

    protected override AIStrategy BuildOverrideStrategy(Node owner)
    {
        return new BasicAttackOnlyAIStrategy
        {
            ForcedAction = ResolveBasicAttackAction(owner),
            PickRandomTarget = RandomTargetSelection
        };
    }

    bool IStatusActionRule.IsActionAllowed(ActionData action, ItemData sourceItem, Node owner, BattleController battleController, out string reason)
    {
        if (!base.IsActionAllowed(action, sourceItem, owner, battleController, out reason))
        {
            return false;
        }

        var requiredAction = ResolveBasicAttackAction(owner);
        if (requiredAction == null || StatusRuleUtils.AreSameAction(action, requiredAction))
        {
            reason = string.Empty;
            return true;
        }

        reason = string.IsNullOrWhiteSpace(ForcedAttackRejectReason)
            ? "Only basic attacks are allowed."
            : ForcedAttackRejectReason;
        return false;
    }

    private ActionData ResolveBasicAttackAction(Node owner)
    {
        if (BasicAttackAction != null) return BasicAttackAction;

        var actionManager = owner?.GetNodeOrNull<ActionManager>(ActionManager.DefaultName);
        if (actionManager == null) return null;

        foreach (var page in actionManager.RootPages)
        {
            if (page == null || page.SubCommands == null) continue;
            foreach (var command in page.SubCommands)
            {
                if (command is ActionData action && IsBasicAttackCandidate(action))
                {
                    return action;
                }
            }
        }

        return actionManager.LearnedActions
            .OfType<ActionData>()
            .FirstOrDefault(IsBasicAttackCandidate);
    }

    private static bool IsBasicAttackCandidate(ActionData action)
    {
        if (action == null) return false;
        if (string.Equals(action.CommandName, "Attack", StringComparison.OrdinalIgnoreCase)) return true;
        return action.Category == ActionCategory.Attack;
    }
}
