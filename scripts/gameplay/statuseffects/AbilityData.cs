using Godot;

/// <summary>
/// Base class for all Ability Resources. An ability is a data-driven object
/// that can contain logic for modifying actions via the IActionModifier interface.
/// </summary>
public partial class AbilityData : Resource, IActionModifier, IPersistentEffect
{
    // By having the base resource implement the interface, all derived
    // ability types automatically become action modifiers.

    // Default implementations for lifecycle hooks. Derived ability resources can override these.
    public virtual void OnApply(Node owner, ActionDirector actionDirector) { }
    public virtual void OnRemove(Node owner, ActionDirector actionDirector) { }
    public virtual void OnTurnEnd(Node owner, ActionDirector actionDirector) { }
    public virtual void OnTurnStart(Node owner, ActionDirector actionDirector) { }
}