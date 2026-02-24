using Godot;
using System;
using System.Linq;

/// <summary>
/// Represents a menu item that opens a sub-menu (e.g., Magic, Items, Geomancy).
/// </summary>
[GlobalClass]
public partial class BattleCategory : BattleCommand
{
    [Export] public MenuTheme Theme { get; set; }

    [Export] public Godot.Collections.Array<BattleCommand> SubCommands { get; set; } = new();

    [ExportGroup("Auto Populate")]
    [Export]
    public Godot.Collections.Array<ActionCategory> AutoPopulateActionCategories { get; set; } = new();

    /// <summary>
    /// Determines whether a learned action should be auto-added to this category.
    /// If category filters are configured, those are used.
    /// Otherwise, falls back to matching by category name for backward compatibility.
    /// </summary>
    public bool MatchesLearnedAction(ActionData action)
    {
        if (action == null) return false;

        if (AutoPopulateActionCategories != null && AutoPopulateActionCategories.Count > 0)
        {
            return AutoPopulateActionCategories.Any(category => category == action.Category);
        }

        var categoryName = CommandName ?? string.Empty;
        var actionName = action.Category.ToString();

        if (string.Equals(categoryName, actionName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (categoryName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            var singular = categoryName[..^1];
            if (string.Equals(singular, actionName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
