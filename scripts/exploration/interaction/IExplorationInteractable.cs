using Godot;

public interface IExplorationInteractable
{
    string InteractionName { get; }
    InteractionTriggerMode TriggerMode { get; }
    Node3D InteractionOrigin { get; }
    bool IsConsumed { get; }

    bool CanInteract(ExplorationInteractionContext context, out string reason);
    void Interact(ExplorationInteractionContext context);
}
