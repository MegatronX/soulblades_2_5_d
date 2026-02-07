using Godot;

/// <summary>
/// Base class for all trigger condition resources. A condition is a reusable,
/// data-driven check that determines if an ability should activate.
/// </summary>
[GlobalClass]
public partial class TriggerCondition : Resource
{
    /// <summary>
    /// Checks if this condition is met by the provided ActionContext.
    /// </summary>
    public virtual bool IsMet(ActionContext context) => true;
}