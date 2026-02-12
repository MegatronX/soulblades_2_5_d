using Godot;

/// <summary>
/// The abstract base class for a single piece of logic within a StatusEffect.
/// Each concrete implementation will handle one specific behavior, like modifying a stat
/// or dealing damage over time. This makes the system highly modular.
/// </summary>
[GlobalClass]
public abstract partial class EffectLogic : Resource
{
    /// <summary>
    /// Called when the status effect is first applied to a target.
    /// </summary>
    public abstract void OnApply(Node target);

    /// <summary>
    /// Called when the status effect is removed from a target.
    /// </summary>
    public abstract void OnRemove(Node target);

    /// <summary>
    /// Called at the start of the owner's turn.
    /// </summary>
    public virtual void OnTurnStart(Node target) { }

    /// <summary>
    /// Called at the end of the owner's turn.
    /// </summary>
    public virtual void OnTurnEnd(Node target) { }
}
