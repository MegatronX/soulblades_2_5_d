using Godot;

/// <summary>
/// Runtime context for map interactions.
/// </summary>
public sealed class ExplorationInteractionContext
{
    public ExplorationInteractionContext(
        ExplorationMapController mapController,
        Node interactor,
        InventoryManager inventoryManager,
        GameManager gameManager)
    {
        MapController = mapController;
        Interactor = interactor;
        InventoryManager = inventoryManager;
        GameManager = gameManager;
    }

    public ExplorationMapController MapController { get; }
    public Node Interactor { get; }
    public InventoryManager InventoryManager { get; }
    public GameManager GameManager { get; }
}
