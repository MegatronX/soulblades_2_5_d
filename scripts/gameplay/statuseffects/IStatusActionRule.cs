using Godot;
using System.Collections.Generic;

/// <summary>
/// Optional hook for statuses that gate actions or rewrite selected targets.
/// BattleController consults these before action execution.
/// </summary>
public interface IStatusActionRule
{
    bool IsActionAllowed(ActionData action, ItemData sourceItem, Node owner, BattleController battleController, out string reason)
    {
        reason = string.Empty;
        return true;
    }

    bool TryRewriteTargets(ActionData action, ItemData sourceItem, Node owner, List<Node> currentTargets, BattleController battleController, IRandomNumberGenerator rng, out List<Node> rewrittenTargets)
    {
        rewrittenTargets = null;
        return false;
    }
}
