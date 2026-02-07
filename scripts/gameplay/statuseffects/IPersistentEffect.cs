using Godot;

/// <summary>
/// Defines a generic contract for any persistent effect (like a status effect or an ability)
/// that needs to hook into character and battle lifecycle events.
/// </summary>
public interface IPersistentEffect
{
    /// <summary>
    /// Called when the effect is first applied to a character (e.g., status is inflicted, ability is equipped).
    /// </summary>
    void OnApply(Node owner, ActionDirector actionDirector);

    /// <summary>
    /// Called when the effect is removed from a character (e.g., status expires, ability is unequipped).
    /// </summary>
    void OnRemove(Node owner, ActionDirector actionDirector);

    /// <summary>
    /// Called at the start of the owner's turn.
    /// </summary>
    void OnTurnStart(Node owner, ActionDirector actionDirector);

    /// <summary>
    /// Called at the end of the owner's turn.
    /// </summary>
    void OnTurnEnd(Node owner, ActionDirector actionDirector);
}