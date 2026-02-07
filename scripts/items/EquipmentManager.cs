using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A component attached to a character that manages their equipped items.
/// It is the source of truth for what a character has equipped in each slot.
/// </summary>
[GlobalClass]
public partial class EquipmentManager : Node
{
    [Signal]
    public delegate void EquipmentChangedEventHandler(EquipmentSlot slot, ItemData newItem);

    [Signal]
    public delegate void SlotAddedEventHandler(EquipmentSlot newSlot);

    private readonly List<EquipmentSlot> _slots = new();
    private Node _owner;
    private StatsComponent _statsComponent;

    public override void _Ready()
    {
        _owner = GetParent();
        _statsComponent = _owner.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        // Find all child nodes of type EquipmentSlot and register them.
        foreach (var child in GetChildren())
        {
            if (child is EquipmentSlot slot)
            {
                _slots.Add(slot);
            }
        }
    }

    /// <summary>
    /// Equips an item to a specific slot instance, unequipping any previous item.
    /// </summary>
    /// <param name="itemToEquip">The ItemData resource to equip.</param>
    /// <param name="targetSlot">The specific slot node to equip the item to.</param>
    /// <returns>The item that was previously in the slot, if any.</returns>
    public ItemData EquipItem(ItemData itemToEquip, EquipmentSlot targetSlot)
    {
        if (itemToEquip == null || targetSlot == null || !_slots.Contains(targetSlot))
        {
            GD.PrintErr("Invalid item or slot provided to EquipItem.");
            return null;
        }

        // --- Slot Type Restriction Check ---
        var equippableComponent = itemToEquip.Components.FirstOrDefault(c => c is EquippableComponentData, null) as EquippableComponentData;
        //OfType<EquippableComponentData>().FirstOrDefault();
        if (equippableComponent == null)
        {
            GD.PrintErr($"Item '{itemToEquip.ItemName}' is not equippable.");
            return null;
        }

        if (equippableComponent.SlotType != targetSlot.SlotType)
        {
            GD.PrintErr($"Cannot equip '{itemToEquip.ItemName}' (Type: {equippableComponent.SlotType}) into a {targetSlot.SlotType} slot.");
            return null; // Abort if the item type doesn't match the slot type.
        }
        // ------------------------------------

        var oldItem = UnequipItem(targetSlot);

        targetSlot.EquippedItem = itemToEquip;
        ApplyItemModifiers(itemToEquip);

        EmitSignal(SignalName.EquipmentChanged, targetSlot, itemToEquip);
        GD.Print($"Equipped '{itemToEquip.ItemName}' to '{targetSlot.Name}' slot.");
        return oldItem;
    }

    /// <summary>
    /// Unequips the item from the specified slot instance.
    /// </summary>
    /// <returns>The item that was unequipped, if any.</returns>
    public ItemData UnequipItem(EquipmentSlot targetSlot)
    {
        if (targetSlot == null || targetSlot.IsEmpty())
        {
            return null;
        }

        var currentItem = targetSlot.EquippedItem;
        targetSlot.EquippedItem = null;

        if (currentItem != null)
        {
            RemoveItemModifiers(currentItem);
            EmitSignal(SignalName.EquipmentChanged, targetSlot, new Variant());
            GD.Print($"Unequipped '{currentItem.ItemName}' from '{targetSlot.Name}' slot.");
            return currentItem;
        }
        return null;
    }

    /// <summary>
    /// Adds a new equipment slot to the character dynamically during gameplay.
    /// </summary>
    public void AddAccessorySlot()
    {
        var newSlot = new EquipmentSlot { Name = $"AccessorySlot{_slots.Count}", SlotType = EquipmentSlotType.Accessory };
        _slots.Add(newSlot);
        AddChild(newSlot);
        EmitSignal(SignalName.SlotAdded, newSlot);
        GD.Print($"Added new accessory slot: {newSlot.Name}");
    }

    private void ApplyItemModifiers(ItemData item)
    {
        if (_statsComponent == null || item == null) return;

        var equippable = item.Components.OfType<EquippableComponentData>().FirstOrDefault();
        if (equippable == null) return;

        foreach (var modifier in equippable.StatBoosts)
        {
            if (modifier.Source == null)
            {
                modifier.Source = item;
            }

            _statsComponent.AddModifier(modifier);
        }
    }

    private void RemoveItemModifiers(ItemData item)
    {
        if (_statsComponent == null || item == null) return;

        var equippable = item.Components.OfType<EquippableComponentData>().FirstOrDefault();
        if (equippable == null) return;
        _statsComponent.RemoveAllModifiersFromSource(item);
    }

    public IReadOnlyList<EquipmentSlot> GetSlots() => _slots;
}