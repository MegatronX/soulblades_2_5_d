using Godot;

/// <summary>
/// Defines a contract for any object (like a status effect or passive ability) that can
/// intercept and modify an ActionContext at various points during its lifecycle.
/// Defines a contract for any object (like a status effect or passive ability) that can intercept
/// and modify an ActionContext at various points during its lifecycle.
/// </summary>
public interface IActionModifier
{
    /// <summary>
    /// Called on the **initiator** of an action, right after the ActionContext is created.
    /// Use for effects like "Charged Strike" that modify your own outgoing action.
    /// This hook modifies the master context before it is copied for each target.
    /// </summary>
    /// <param name="context">The action being initiated. Changes affect all targets.</param>
    /// <param name="owner">The character initiating the action.</param>
    void OnActionInitiated(ActionContext context, Node owner) { }

    /// <summary>
    /// Called on **every combatant** in the battle after the "Initiated" hooks.
    /// Use for global redirection effects like "Storm Drain" that can hijack an action
    /// regardless of the original target. This hook modifies the master context.
    /// </summary>
    /// <param name="context">The master action context. Modifiers can change the target list.</param>
    /// <param name="owner">The character who has this modifier.</param>
    void OnActionBroadcast(ActionContext context, Node owner) { }

    /// <summary>
    /// Called on an **ally of the initiator** when the action is created.
    /// Use for effects like "Pack Hunter" or "Commander's Aura" that buff an ally's action.
    /// This hook modifies the master context.
    /// </summary>
    /// <param name="context">The action being initiated by an ally.</param>
    /// <param name="initiator">The ally performing the action.</param>
    /// <param name="owner">The character who has this modifier.</param>
    void OnAllyActionInitiated(ActionContext context, Node initiator, Node owner) { }

    /// <summary>
    /// Called on the **target** of an action, just before execution against them.
    /// Called on the **target** of an action (and its allies), just before execution against them.
    /// Use for effects like "Reflect," "Dodge," or elemental resistances.
    /// This hook modifies a per-target copy of the context.
    /// </summary>
    /// <param name="context">The per-target copy of the action. Changes only affect this target.</param>
    /// <param name="owner">The character being targeted.</param>
    void OnActionTargeted(ActionContext context, Node owner) { }

    /// <summary>
    /// Allows a modifier to expand or restrict the allowed targeting modes for an action.
    /// </summary>
    /// <param name="currentAllowed">The currently allowed targeting flags.</param>
    /// <returns>The modified targeting flags.</returns>
    TargetingType ModifyAllowedTargeting(TargetingType currentAllowed) => currentAllowed;

    /// <summary>
    /// Called after the action has been fully calculated and applied (damage dealt, etc.).
    /// Use this for reactive effects like "Counter Attack" that depend on the outcome (Hit/Miss/Crit).
    /// </summary>
    /// <param name="context">The action context for this target.</param>
    /// <param name="owner">The character who has this modifier.</param>
    /// <param name="result">The final calculated result of the action against the owner.</param>
    void OnActionPostExecution(ActionContext context, Node owner, ActionResult result) { }

    // In C# 8.0+, interfaces can have default implementations, allowing us to add new methods
    // without breaking existing classes that implement the interface.
}