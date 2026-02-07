using Godot;

/// <summary>
/// A component that allows an action to chain into another action if specific
/// conditions are met after the initial action resolves.
/// </summary>
[GlobalClass]
public partial class ChainActionComponentData : ActionComponentData
{
    /// <summary>
    /// The ActionData resource for the action that will be triggered if the conditions are met.
    /// </summary>
    [Export(PropertyHint.ResourceType, "ActionData")]
    public ActionData ChainedAction { get; private set; }

    [Export]
    public Godot.Collections.Array<TriggerCondition> Conditions { get; private set; } = new();
}