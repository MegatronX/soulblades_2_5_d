using Godot;
using System.Collections.Generic;

/// <summary>
/// Base class for AI decision-making logic.
/// </summary>
[GlobalClass]
public abstract partial class AIStrategy : Resource
{
    /// <summary>
    /// Calculates the best action to take for the current turn.
    /// </summary>
    /// <param name="controller">The AI controller requesting the decision.</param>
    /// <param name="user">The character controlled by the AI.</param>
    /// <param name="battleController">Reference to the battle controller for context (enemies, allies).</param>
    /// <returns>A BattleDecision containing the action and targets.</returns>
    public abstract BattleDecision GetDecision(AIController controller, Node user, BattleController battleController);
}
