using Godot;

/// <summary>
/// Represents a menu item that opens a sub-menu (e.g., Magic, Items, Geomancy).
/// </summary>
[GlobalClass]
public partial class BattleCategory : BattleCommand
{
    [Export] public MenuTheme Theme { get; set; }
    
    [Export] public Godot.Collections.Array<BattleCommand> SubCommands { get; set; } = new();
}