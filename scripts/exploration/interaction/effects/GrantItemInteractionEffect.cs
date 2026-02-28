using Godot;

/// <summary>
/// Grants an item to inventory.
/// </summary>
[GlobalClass]
public partial class GrantItemInteractionEffect : InteractionEffect
{
    [Export]
    public ItemData Item { get; private set; }

    [Export(PropertyHint.Range, "1,999,1")]
    public int Quantity { get; private set; } = 1;

    public override void Execute(ExplorationInteractionContext context, Node source)
    {
        if (Item == null || Quantity <= 0) return;
        context?.InventoryManager?.AddItem(Item, Quantity);
    }
}
