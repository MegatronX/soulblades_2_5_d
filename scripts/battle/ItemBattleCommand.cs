using Godot;

/// <summary>
/// Runtime battle command representing an inventory item used as an action.
/// </summary>
[GlobalClass]
public partial class ItemBattleCommand : BattleCommand
{
    [Export] public ItemData Item { get; set; }
    [Export] public ActionData Action { get; set; }
    [Export] public int Quantity { get; set; }
    [Export] public bool IsUsable { get; set; }
}
