using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A simple AI that always uses a specific action on a random valid target.
/// </summary>
[GlobalClass]
public partial class DummyAIStrategy : AIStrategy
{
    [Export]
    public ActionData ActionToUse { get; set; }

    public override BattleDecision GetDecision(AIController controller, Node user, BattleController battleController)
    {
        if (ActionToUse == null) return null;

        var decision = new BattleDecision { Action = ActionToUse };
        var potentialTargets = new List<Node>();

        // Determine valid targets based on action type
        if (ActionToUse.AllowedTargeting.HasFlag(TargetingType.AnyEnemy))
        {
            potentialTargets.AddRange(battleController.GetOpponents(user));
        }
        else if (ActionToUse.AllowedTargeting.HasFlag(TargetingType.AnyAlly))
        {
            potentialTargets.AddRange(battleController.GetAllies(user));
        }

        if (potentialTargets.Count > 0)
        {
            // Pick random
            var rng = new RandomNumberGenerator();
            rng.Randomize();
            decision.Targets.Add(potentialTargets[rng.RandiRange(0, potentialTargets.Count - 1)]);
        }

        return decision;
    }
}
