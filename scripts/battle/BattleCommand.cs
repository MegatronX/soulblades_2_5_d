using Godot;

/// <summary>
/// Base class for any item that appears in the battle menu (Action or Sub-Menu).
/// </summary>
[GlobalClass]
public partial class BattleCommand : Resource
{
    [Export] public string CommandName { get; set; } = "Command";
    [Export] public Texture2D Icon { get; set; }
    [Export] public string Description { get; set; }
}