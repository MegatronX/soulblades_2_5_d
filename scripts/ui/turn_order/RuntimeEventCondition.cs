using Godot;

/// <summary>
/// A trigger condition that checks if a specific runtime event occurred during
/// the action's execution (e.g., a successful timed hit).
/// </summary>
[GlobalClass]
public partial class RuntimeEventCondition : TriggerCondition
{
    [Export]
    public string EventTag { get; private set; }

    public override bool IsMet(ActionContext context)
    {
        if (string.IsNullOrEmpty(EventTag))
        {
            return false;
        }
        return context.RuntimeEvents.Contains(EventTag);
    }
}