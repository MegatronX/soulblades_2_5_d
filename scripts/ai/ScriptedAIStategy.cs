using Godot;
using System.Linq;

/// <summary>
/// Defines a single phase of a scripted AI.
/// </summary>
[GlobalClass]
public partial class AIPhase : Resource
{
    [Export] public Godot.Collections.Array<ActionData> PossibleActions { get; set; } = new();
    [Export] public int DurationTurns { get; set; } = 1;
}

/// <summary>
/// An AI that cycles through defined phases.
/// </summary>
[GlobalClass]
public partial class ScriptedAIStrategy : AIStrategy
{
    [Export] public Godot.Collections.Array<AIPhase> Phases { get; set; } = new();
    [Export] public bool Loop { get; set; } = true;

    public override BattleDecision GetDecision(AIController controller, Node user, BattleController battleController)
    {
        if (Phases.Count == 0) return null;

        // Retrieve state from controller memory
        int currentPhaseIndex = controller.GetMemory<int>("Script_PhaseIndex", 0);
        int turnsInPhase = controller.GetMemory<int>("Script_TurnsInPhase", 0);

        var currentPhase = Phases[currentPhaseIndex];

        // Pick an action from the current phase
        ActionData chosenAction = null;
        if (currentPhase.PossibleActions.Count > 0)
        {
            var rng = new RandomNumberGenerator();
            rng.Randomize();
            chosenAction = currentPhase.PossibleActions[rng.RandiRange(0, currentPhase.PossibleActions.Count - 1)];
        }

        // Advance logic
        turnsInPhase++;
        if (turnsInPhase >= currentPhase.DurationTurns)
        {
            turnsInPhase = 0;
            currentPhaseIndex++;
            if (currentPhaseIndex >= Phases.Count)
            {
                currentPhaseIndex = Loop ? 0 : Phases.Count - 1;
            }
        }

        // Save state back
        controller.Memory["Script_PhaseIndex"] = currentPhaseIndex;
        controller.Memory["Script_TurnsInPhase"] = turnsInPhase;

        if (chosenAction == null) return null;

        // Simple target selection (Random Opponent) - could be expanded
        var decision = new BattleDecision { Action = chosenAction };
        var opponents = battleController.GetOpponents(user).ToList();
        if (opponents.Count > 0)
        {
            var rng = new RandomNumberGenerator();
            decision.Targets.Add(opponents[rng.RandiRange(0, opponents.Count - 1)]);
        }

        return decision;
    }
}
