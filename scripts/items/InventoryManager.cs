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
    public const string Path = "/root/InventoryManager";
    public const string NodeName = "InventoryManager";

    [Signal]
    public delegate void InventoryChangedEventHandler();

    [Signal]
    public delegate void ItemConsumedEventHandler(ItemData item, int quantity, int remaining);

    // The core data structure for the inventory. Maps an item resource to its quantity.
    private readonly Dictionary<ItemData, int> _inventory = new();

    /// <summary>
    /// Adds a specified quantity of an item to the inventory.
    /// </summary>
    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;

        var resolved = ResolveInventoryItem(item) ?? item;

        if (_inventory.ContainsKey(resolved))
        {
            _inventory[resolved] += quantity;
        }
        else
        {
            _inventory[resolved] = quantity;
        }
        
        GD.Print($"Added {quantity}x {resolved.ItemName}. New total: {_inventory[resolved]}");
        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Removes a specified quantity of an item from the inventory.
    /// </summary>
    /// <returns>True if the items were successfully removed, false otherwise.</returns>
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        var resolved = ResolveInventoryItem(item);
        if (resolved == null || quantity <= 0 || !_inventory.ContainsKey(resolved) || _inventory[resolved] < quantity)
        {
            return false; // Cannot remove item.
        }

        _inventory[resolved] -= quantity;
        GD.Print($"Removed {quantity}x {resolved.ItemName}. New total: {_inventory[resolved]}");

        if (_inventory[resolved] == 0)
        {
            _inventory.Remove(resolved);
        }

        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    /// <summary>
    /// Attempts to consume an item based on a chance. Returns true if the item was consumed.
    /// </summary>
    public bool TryConsumeItem(ItemData item, int quantity = 1, float? overrideChancePercent = null, IRandomNumberGenerator rng = null)
    {
        if (item == null || quantity <= 0) return false;

        var resolved = ResolveInventoryItem(item) ?? item;
        var consumable = resolved.Components?.OfType<ConsumableComponentData>().FirstOrDefault();
        if (consumable == null) return false;

        float chance = overrideChancePercent ?? consumable.ActionConsumeChancePercent;
        chance = Mathf.Clamp(chance, 0f, 100f);

        if (chance <= 0f) return false;
        if (chance < 100f)
        {
            float roll = rng != null ? rng.RandRangeFloat(0f, 100f) : GD.Randf() * 100f;
            if (roll > chance) return false;
        }

        if (!RemoveItem(resolved, quantity)) return false;

        EmitSignal(SignalName.ItemConsumed, resolved, quantity, GetItemCount(resolved));
        return true;
    }

    /// <summary>
    /// Returns a list of items that can be used as actions in the given context.
    /// </summary>
    public List<ItemData> GetActionUsableItems(bool inBattle, Node user = null)
    {
        return _inventory.Keys
            .Where(item => item != null && IsItemActionUsableInternal(item, inBattle, user))
            .ToList();
    }

    public bool IsItemActionUsable(ItemData item, bool inBattle, Node user = null)
    {
        return IsItemActionUsableInternal(item, inBattle, user);
    }

    private static bool IsItemActionUsableInternal(ItemData item, bool inBattle, Node user)
    {
        var consumable = item.Components?.OfType<ConsumableComponentData>().FirstOrDefault();
        if (consumable == null || consumable.ActionToPerform == null) return false;

        if (consumable.AlwaysUsableAsAction) return true;

        if (inBattle && !consumable.UsableInBattle) return false;
        if (!inBattle && !consumable.UsableInMenu) return false;

        if (consumable.ActionUseRequirements != null && !consumable.ActionUseRequirements.IsSatisfied(user))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the current quantity of a specific item in the inventory.
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        var resolved = ResolveInventoryItem(item);
        return resolved == null ? 0 : _inventory.GetValueOrDefault(resolved, 0);
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

    private ItemData ResolveInventoryItem(ItemData item)
    {
        if (item == null) return null;
        if (_inventory.ContainsKey(item)) return item;

        var path = item.ResourcePath;
        if (string.IsNullOrEmpty(path)) return null;

        foreach (var key in _inventory.Keys)
        {
            if (key != null && key.ResourcePath == path) return key;
        }

        return null;
    }
}
