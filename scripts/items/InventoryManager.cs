using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A global singleton (Autoload) that manages the player's inventory.
/// It tracks all items and their quantities.
/// </summary>
[GlobalClass]
public partial class InventoryManager : Node
{
    [Signal]
    public delegate void InventoryChangedEventHandler();

    // The core data structure for the inventory. Maps an item resource to its quantity.
    private readonly Dictionary<ItemData, int> _inventory = new();

    /// <summary>
    /// Adds a specified quantity of an item to the inventory.
    /// </summary>
    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;

        if (_inventory.ContainsKey(item))
        {
            _inventory[item] += quantity;
        }
        else
        {
            _inventory[item] = quantity;
        }
        
        GD.Print($"Added {quantity}x {item.ItemName}. New total: {_inventory[item]}");
        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Removes a specified quantity of an item from the inventory.
    /// </summary>
    /// <returns>True if the items were successfully removed, false otherwise.</returns>
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0 || !_inventory.ContainsKey(item) || _inventory[item] < quantity)
        {
            return false; // Cannot remove item.
        }

        _inventory[item] -= quantity;
        GD.Print($"Removed {quantity}x {item.ItemName}. New total: {_inventory[item]}");

        if (_inventory[item] == 0)
        {
            _inventory.Remove(item);
        }

        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    /// <summary>
    /// Gets the current quantity of a specific item in the inventory.
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        return _inventory.GetValueOrDefault(item, 0);
    }

    /// <summary>
    /// Returns a list of all items in the inventory that can be equipped to a specific slot.
    /// </summary>
    public List<ItemData> GetEquippableItemsForSlot(EquipmentSlotType slotType)
    {
        return _inventory.Keys
            .Where(item => item.Components
                .OfType<EquippableComponentData>()
                .Any(comp => comp.SlotType == slotType))
            .ToList();
    }

    /// <summary>
    /// Returns a list of all items in the inventory that are consumable.
    /// </summary>
    public List<ItemData> GetConsumableItems()
    {
        return _inventory.Keys
            .Where(item => item.Components
                .OfType<ConsumableComponentData>()
                .Any())
            .ToList();
    }

    /// <summary>
    /// Returns a read-only dictionary of the entire inventory.
    /// Useful for UI elements that need to display all items.
    /// </summary>
    public IReadOnlyDictionary<ItemData, int> GetInventory()
    {
        return _inventory;
    }
}