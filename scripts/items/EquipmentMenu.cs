using Godot;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

/// <summary>
/// Manages the UI for equipping items to a character.
/// This script orchestrates the display of equipment slots, item lists, and stat changes.
/// </summary>
public partial class EquipmentMenu : Control
{
    // --- UI Node References (assign these in the editor) ---
    [Export] private VBoxContainer _slotContainer; // The container holding your slot buttons.
    [Export] private ItemList _itemList; // The list that will show available items.
    [Export] private Label _itemNameLabel;
    [Export] private RichTextLabel _itemDescriptionLabel;
    [Export] private StatComparisonPanel _statComparisonPanel;

    [Export]
    private PackedScene EquipmentSlotUIScene;
    // ... other UI references for stats panels ...

    private EquipmentManager _characterEquipment;
    private InventoryManager _inventoryManager; // Assume this is a singleton.

    private EquipmentSlot _selectedSlot;
    private List<ItemData> _currentlyListedItems;

    public override void _Ready()
    {
        _inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");
        
        // Connect signals for the UI elements.
        _itemList.ItemSelected += OnListItemSelected;
        _itemList.ItemActivated += OnListItemActivated; // Double-click to equip.

        // This assumes your slot buttons are direct children of the container.
        foreach (var child in _slotContainer.GetChildren())
        {
            if (child is EquipmentSlotUI slotUI)
            {
                // The button's name should match the EquipmentSlot node's name.
                slotUI.SlotSelected += OnSlotSelected;
            }
        }
    }

    /// <summary>
    /// Opens the menu for a specific character.
    /// </summary>
    public void OpenForCharacter(Node character)
    {
        _characterEquipment = character.GetNode<EquipmentManager>("EquipmentManager");
        if (_characterEquipment == null)
        {
            GD.PrintErr($"Character '{character.Name}' does not have an EquipmentManager!");
            return;
        }

        // Disconnect first to prevent duplicate connections if the menu is re-opened for the same character.
        if (_characterEquipment.IsConnected(EquipmentManager.SignalName.EquipmentChanged, Callable.From<EquipmentSlot, ItemData>(OnEquipmentChanged)))
        {
            _characterEquipment.EquipmentChanged -= OnEquipmentChanged;
        }
        _characterEquipment.EquipmentChanged += OnEquipmentChanged;
        _statComparisonPanel?.LinkToCharacter(character);
        RefreshAllSlots();
        
        // Select the first slot by default.
        var firstSlotUI = _slotContainer.GetChildren().OfType<EquipmentSlotUI>().FirstOrDefault();
        if (firstSlotUI != null)
        {
            OnSlotSelected(firstSlotUI);
        }
        this.Show();
    }

    private void OnSlotSelected(EquipmentSlotUI slotUI)
    {
        _selectedSlot = slotUI.LinkedSlot;
        if (_selectedSlot == null)
        {
            GD.PrintErr($"The UI slot '{slotUI.Name}' is not linked to a data slot.");
            return;
        }
        GD.Print($"Selected slot: {_selectedSlot.Name}");

        // Populate the item list with items from inventory that match the selected slot.
        _itemList.Clear();
        _currentlyListedItems = _inventoryManager.GetEquippableItemsForSlot(_selectedSlot.SlotType);

        _currentlyListedItems.ForEach(item => _itemList.AddItem(item.ItemName)); //, item.Icon));
    }

    private void OnListItemSelected(long index)
    {
        var selectedItem = _currentlyListedItems[(int)index];
        _itemNameLabel.Text = selectedItem.ItemName;
        _itemDescriptionLabel.Text = selectedItem.Description;

        // Update the stat preview panel.
        _statComparisonPanel?.UpdatePreview(_selectedSlot?.EquippedItem, selectedItem, true, false);

        // Here, you would also update the StatComparisonPanel to show potential stat changes.
    }

    private void OnListItemActivated(long index)
    {
        if (_selectedSlot == null) return;

        var itemToEquip = _currentlyListedItems[(int)index];
        
        // Tell the character's equipment manager to equip the new item.
        var unequippedItem = _characterEquipment.EquipItem(itemToEquip, _selectedSlot);

        // Add the previously equipped item (if any) back to the inventory.
        if (unequippedItem != null)
        {
            _inventoryManager.AddItem(unequippedItem);
        }

        _statComparisonPanel?.UpdatePreview(_selectedSlot?.EquippedItem, _selectedSlot?.EquippedItem, true, true);

        // Remove the newly equipped item from the inventory.
        _inventoryManager.RemoveItem(itemToEquip);
    }

    private void OnEquipmentChanged(EquipmentSlot slot, ItemData newItem)
    {
        // When equipment changes, refresh the UI for that slot.
        // We defer the refresh to ensure that the inventory operations in OnListItemActivated
        // have completed before we rebuild the item list.
        CallDeferred(MethodName.RefreshSlotUI, slot);
    }

    private void RefreshSlotUI(EquipmentSlot slot)
    {
        GD.Print($"Equipment changed in slot {slot.Name}. Refreshing UI.");
        var slotUI = _slotContainer.GetChildren().OfType<EquipmentSlotUI>().FirstOrDefault(ui => ui.LinkedSlot == slot);
        
        if (slotUI == null) return;

        slotUI.Refresh();
        OnSlotSelected(slotUI); // Re-select to refresh the item list.
    }

    private void RefreshAllSlots()
    {
        // 1. Clear out all old UI nodes.
        RemoveAllChildren(_slotContainer);

        // 2. Create a new UI node for each data slot the character has.
        foreach (var slot in _characterEquipment.GetSlots())
        {
            // Instantiate a new UI scene for the slot.
            var newSlotUI = EquipmentSlotUIScene.Instantiate<EquipmentSlotUI>();
            
            // Link the new UI node to its corresponding data slot and connect its signal.
            newSlotUI.LinkToSlot(slot);
            newSlotUI.SlotSelected += OnSlotSelected;
            
            // Add the new UI node to the container.
            _slotContainer.AddChild(newSlotUI);
        }
    }

    /// <summary>
/// Safely removes and deletes all child nodes from a given parent node.
/// </summary>
private void RemoveAllChildren(Node parentNode)
{
    foreach (var child in parentNode.GetChildren())
    {
        child.QueueFree();
    }
}
}