using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI strategy that always attempts to use a basic attack action.
/// </summary>
[GlobalClass]
public partial class BasicAttackOnlyAIStrategy : AIStrategy
{
    [Export(PropertyHint.ResourceType, "ActionData")]
    public ActionData ForcedAction { get; set; }

    [Export]
    public bool PickRandomTarget { get; set; } = false;

    public override BattleDecision GetDecision(AIController controller, Node user, BattleController battleController)
    {
        if (controller == null || user == null || battleController == null) return null;

        var action = ResolveAction(controller);
        if (action == null) return null;
        if (!battleController.IsActionAllowedForActor(user, action, null, out _)) return null;

        var target = ChooseTarget(action, user, battleController);
        if (target == null) return null;

        return new BattleDecision
        {
            Action = action,
            Targets = new List<Node> { target }
        };
    }

    private ActionData ResolveAction(AIController controller)
    {
        if (ForcedAction != null) return ForcedAction;

        var actionManager = controller.GetActionManager();
        if (actionManager == null) return null;

        var candidates = new List<ActionData>();
        foreach (var page in actionManager.RootPages)
        {
            if (page == null || page.SubCommands == null) continue;
            foreach (var command in page.SubCommands)
            {
                if (command is ActionData action)
                {
                    candidates.Add(action);
                }
            }
        }

        candidates.AddRange(actionManager.LearnedActions.OfType<ActionData>());

        var byName = candidates.FirstOrDefault(IsBasicAttackNamed);
        if (byName != null) return byName;

        return candidates.FirstOrDefault(a => a != null && a.Category == ActionCategory.Attack);
    }

    private Node ChooseTarget(ActionData action, Node user, BattleController battleController)
    {
        var candidates = CollectTargets(action, user, battleController);
        if (candidates.Count == 0) return null;

        if (!PickRandomTarget || candidates.Count == 1)
        {
            return candidates[0];
        }

        var rng = new RandomNumberGenerator();
        rng.Randomize();
        return candidates[rng.RandiRange(0, candidates.Count - 1)];
    }

    private static List<Node> CollectTargets(ActionData action, Node user, BattleController battleController)
    {
        var result = new List<Node>();
        if (action == null || user == null || battleController == null) return result;

        bool includeSelf = action.AllowedTargeting.HasFlag(TargetingType.Self);
        bool includeAllies = action.AllowedTargeting.HasFlag(TargetingType.AnyAlly)
            || action.AllowedTargeting.HasFlag(TargetingType.AnySingleTarget)
            || action.AllowedTargeting.HasFlag(TargetingType.OwnParty)
            || action.AllowedTargeting.HasFlag(TargetingType.AnyAllyParty)
            || action.AllowedTargeting.HasFlag(TargetingType.AnySingleParty);
        bool includeEnemies = action.AllowedTargeting.HasFlag(TargetingType.AnyEnemy)
            || action.AllowedTargeting.HasFlag(TargetingType.AnySingleTarget)
            || action.AllowedTargeting.HasFlag(TargetingType.AnyEnemyParty)
            || action.AllowedTargeting.HasFlag(TargetingType.AnySingleParty);

        if (includeEnemies)
        {
            AddTargets(result, battleController.GetOpponents(user), action.CanTargetDead);
        }

        if (includeSelf)
        {
            AddTargets(result, new[] { user }, action.CanTargetDead);
        }

        if (includeAllies)
        {
            AddTargets(result, battleController.GetAllies(user), action.CanTargetDead);
        }

        return result;
    }

    private static void AddTargets(List<Node> result, IEnumerable<Node> candidates, bool canTargetDead)
    {
        if (result == null || candidates == null) return;

        foreach (var node in candidates)
        {
            if (node == null) continue;
            if (!canTargetDead && !IsAlive(node)) continue;
            if (result.Contains(node)) continue;
            result.Add(node);
        }
    }

    private static bool IsAlive(Node node)
    {
        var stats = node?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        return stats == null || stats.CurrentHP > 0;
    }

    private static bool IsBasicAttackNamed(ActionData action)
    {
        return action != null
            && !string.IsNullOrWhiteSpace(action.CommandName)
            && string.Equals(action.CommandName, "Attack", StringComparison.OrdinalIgnoreCase);
    }
}
