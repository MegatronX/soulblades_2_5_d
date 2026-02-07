using Godot;

/// <summary>
/// A dedicated UI control for a single equipment slot. It contains a Label for the
/// slot name and a Button for the equipped item. It holds a direct reference to an
/// EquipmentSlot data node on a character and updates its appearance accordingly.
/// </summary>
[GlobalClass]
public partial class EquipmentSlotUI : Control
{
    [Signal]
    public delegate void SlotSelectedEventHandler(EquipmentSlotUI slotUI);

    [ExportGroup("Node References")]
    [Export] public Label slotNameLabel;
    [Export] public Button itemButton;

    [ExportGroup("Data Linking")]
    /// <summary>
    /// Assign this in the Godot Editor by dragging the character's EquipmentSlot node here.
    /// </summary>
    [Export]
    public NodePath EquipmentSlotPath { get; set; }

    public EquipmentSlot LinkedSlot { get; private set; }

    public override void _Ready()
    {
        itemButton.Pressed += () => EmitSignal(SignalName.SlotSelected, this);
    }

    public void setLabelName(string name)
    {
        slotNameLabel.Text = name;
    }

    public void setButtonText(string name)
    {
        itemButton.Text = name;
       
    }

    public void setButtonIcon(Texture2D icon)
    {
        itemButton.Icon = icon;
    }
   

    public void LinkToSlot(EquipmentSlot slot)
    {
        LinkedSlot = slot;
        Refresh();
    }

    public void Refresh()
    {
        if (LinkedSlot == null) return;

        // The label now shows the name of the slot itself (e.g., "Weapon", "Accessory1").
        slotNameLabel.Text = LinkedSlot.Name;
        itemButton.Text = LinkedSlot.EquippedItem?.ItemName ?? "(Empty)";
        itemButton.Icon = LinkedSlot.EquippedItem?.Icon;
    }
}