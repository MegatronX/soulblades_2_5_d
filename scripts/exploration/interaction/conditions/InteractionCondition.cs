using Godot;

/// <summary>
/// Base condition resource for exploration interactions.
/// </summary>
[GlobalClass]
public partial class InteractionCondition : Resource
{
    public virtual bool IsSatisfied(ExplorationInteractionContext context, out string reason)
    {
        reason = string.Empty;
        return true;
    }
}
