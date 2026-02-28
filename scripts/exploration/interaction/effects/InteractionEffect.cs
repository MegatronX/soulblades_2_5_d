using Godot;

/// <summary>
/// Base effect resource for exploration interactions.
/// </summary>
[GlobalClass]
public partial class InteractionEffect : Resource
{
    public virtual void Execute(ExplorationInteractionContext context, Node source)
    {
    }
}
