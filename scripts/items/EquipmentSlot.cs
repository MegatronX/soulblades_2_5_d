using Godot;

/// <summary>
/// A node representing a single, unique equipment slot on a character.
/// It holds its type and a reference to the item equipped in it.
/// </summary>
[GlobalClass]
public partial class EquipmentSlot : Node
{
    [Export]
    public EquipmentSlotType SlotType { get; set; }

    public ItemData EquippedItem { get; set; }

    public bool IsEmpty() => EquippedItem == null;
}