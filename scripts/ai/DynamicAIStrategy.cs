using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A dynamic AI that evaluates actions based on utility scores.
/// </summary>
[GlobalClass]
public partial class DynamicAIStrategy : AIStrategy
{
    [Export(PropertyHint.Range, "0.0, 1.0")] public float Intelligence { get; set; } = 1.0f; // 1.0 = Always picks best, 0.0 = Random

    [Export] public Godot.Collections.Array<AIEvaluator> Evaluators { get; set; } = new();

    public override BattleDecision GetDecision(AIController controller, Node user, BattleController battleController)
    {
        var actionManager = controller.GetActionManager();
        if (actionManager == null) return null;

        var opponents = battleController.GetOpponents(user).ToList();
        var allies = battleController.GetAllies(user).ToList();
        
        // Flatten all available actions
        var availableActions = new List<ActionData>();
        foreach (var page in actionManager.RootPages)
        {
            if (page == null) continue;
            foreach (var cmd in page.SubCommands)
            {
                if (cmd is ActionData ad) availableActions.Add(ad);
            }
        }
        availableActions.AddRange(actionManager.LearnedActions.OfType<ActionData>());

        // Score all possible Action + Target combinations
        var scoredDecisions = new List<(float Score, BattleDecision Decision, string DebugInfo)>();

        foreach (var action in availableActions)
        {
            if (battleController != null && !battleController.IsActionAllowedForActor(user, action, null, out _))
            {
                continue;
            }

            // Determine valid targets
            var potentialTargets = new List<Node>();
            if (action.AllowedTargeting.HasFlag(TargetingType.AnyEnemy)) potentialTargets.AddRange(opponents);
            if (action.AllowedTargeting.HasFlag(TargetingType.AnyAlly)) potentialTargets.AddRange(allies);
            if (action.AllowedTargeting.HasFlag(TargetingType.Self)) potentialTargets.Add(user);

            foreach (var target in potentialTargets)
            {
                float score = EvaluateAction(action, user, target, controller);
                string debugInfo = $"{action.CommandName} -> {target.Name}";
                scoredDecisions.Add((score, new BattleDecision { Action = action, Targets = new List<Node> { target } }, debugInfo));
            }
        }

        if (scoredDecisions.Count == 0) return null;

        // Sort by score descending
        scoredDecisions.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Log top 5 choices to memory for debugging
        var logLines = scoredDecisions.Take(5).Select(x => $"[{x.Score:000}] {x.DebugInfo}");
        controller.Memory["DecisionLog"] = string.Join("\n", logLines);

        // Trigger AI Barks if criteria is met
        foreach (var eval in Evaluators)
        {
            if (!string.IsNullOrEmpty(eval.BarkMessage) && scoredDecisions.Count > 0 && scoredDecisions[0].Score >= eval.BarkThreshold)
            {
                var eventBus = battleController.GetNode<GlobalEventBus>(GlobalEventBus.Path);
                eventBus.EmitSignal(GlobalEventBus.SignalName.AIShouted, eval.BarkMessage);
            }
        }

        // Apply Intelligence/Difficulty
        // If intelligence is low, we might pick a sub-optimal action from the top N
        if (Intelligence < 1.0f && scoredDecisions.Count > 1)
        {
            int range = Mathf.Max(1, Mathf.RoundToInt(scoredDecisions.Count * (1.0f - Intelligence)));
            var rng = new RandomNumberGenerator();
            rng.Randomize();
            int index = rng.RandiRange(0, range);
            if (index < scoredDecisions.Count) return scoredDecisions[index].Decision;
        }

        return scoredDecisions[0].Decision;
    }

    private float EvaluateAction(ActionData action, Node user, Node target, AIController controller)
    {
        float score = 0f;

        foreach (var evaluator in Evaluators)
        {
            score += evaluator.Evaluate(action, user, target, controller);
        }

        return score;
    }

    private bool IsAlly(Node a, Node b)
    {
        // Simple group check
        return a.IsInGroup(GameGroups.PlayerCharacters) == b.IsInGroup(GameGroups.PlayerCharacters);
    }
}
