using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Component attached to a character that defines their available battle commands.
/// </summary>
[GlobalClass]
public partial class ActionManager : Node
{
    [Signal]
    public delegate void ActionLearnedEventHandler(ActionData action);

    [Signal]
    public delegate void ActionForgottenEventHandler(ActionData action);

    public const string DefaultName = "ActionManager";

    // We organize root commands into "Pages" for the L/R flipping mechanic.
    // Each entry here represents a page (e.g., Page 1: [Attack, Magic], Page 2: [Summon, Item]).
    // We use BattleCategory to represent a Page because it already holds a list of commands and a theme.
    [Export]
    public Godot.Collections.Array<BattleCategory> RootPages { get; set; } = new();

    [Export]
    public Godot.Collections.Array<BattleCommand> LearnedActions { get; private set; } = new();

    // Dictionary to map command names (e.g. "Attack") to their runtime replacements.
    private Dictionary<string, BattleCommand> _commandOverrides = new();

    /// <summary>
    /// Retrieves the commands for a specific page index.
    /// If a command is a Category, it populates it with matching learned actions.
    /// </summary>
    public List<BattleCommand> GetCommandsForPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= RootPages.Count) return new List<BattleCommand>();
        
        var pageCategory = RootPages[pageIndex];
        if (pageCategory == null) return new List<BattleCommand>();
        var commands = new List<BattleCommand>();

        foreach (var cmd in pageCategory.SubCommands)
        {
            if (cmd == null)
            {
                continue;
            }
            // Determine if this command is overridden (e.g. "Attack" replaced by "Fire Slash")
            var effectiveCommand = cmd;
            if (_commandOverrides.TryGetValue(cmd.CommandName, out var overrideCmd))
            {
                effectiveCommand = overrideCmd;
            }

            // If the command is a category (e.g. "Magic"), we want to return it
            // but we also want to ensure it knows about the learned actions that belong to it.
            // The BattleMenuController will handle expanding it.
            if (effectiveCommand is BattleCategory subCategory)
            {
                // We create a runtime copy to avoid modifying the Resource on disk
                var runtimeCategory = (BattleCategory)subCategory.Duplicate();
                PopulateCategory(runtimeCategory);
                commands.Add(runtimeCategory);
            }
            else
            {
                commands.Add(effectiveCommand);
            }
        }

        return commands;
    }
    
    /// <summary>
    /// Gets the visual theme for the specified page.
    /// </summary>
    public MenuTheme GetPageTheme(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= RootPages.Count) return null;
        return RootPages[pageIndex]?.Theme;
    }

    public bool LearnAction(ActionData action)
    {
        if (!LearnedActions.Contains(action))
        {
            LearnedActions.Add(action);
            EmitSignal(SignalName.ActionLearned, action);
            return true;
        }
        return false;
    }

    public void ForgetAction(ActionData action)
    {
        if (LearnedActions.Remove(action))
            EmitSignal(SignalName.ActionForgotten, action);
    }

    /// <summary>
    /// Overrides a standard command (like "Attack") with a new one (like "Mug").
    /// </summary>
    public void SetCommandOverride(string originalCommandName, BattleCommand newCommand)
    {
        if (newCommand == null)
        {
            _commandOverrides.Remove(originalCommandName);
        }
        else
        {
            _commandOverrides[originalCommandName] = newCommand;
        }
    }

    public void ClearCommandOverride(string originalCommandName) => _commandOverrides.Remove(originalCommandName);

    /// <summary>
    /// Adds a new page (category) to the root list.
    /// Tries to fill the first null slot after the primary page (index 0), otherwise appends.
    /// </summary>
    public void AddPage(BattleCategory page)
    {
        if (page == null || RootPages.Contains(page)) return;

        // Start looking from index 1 (preserve primary page)
        for (int i = 1; i < RootPages.Count; i++)
        {
            if (RootPages[i] == null)
            {
                RootPages[i] = page;
                return;
            }
        }

        // No empty slots found, append.
        RootPages.Add(page);
    }

    public void RemovePage(BattleCategory page)
    {
        RootPages.Remove(page);
    }

    private void PopulateCategory(BattleCategory category)
    {
        // 1. Apply overrides to existing static commands in this category
        for (int i = 0; i < category.SubCommands.Count; i++)
        {
            var cmd = category.SubCommands[i];
            if (_commandOverrides.TryGetValue(cmd.CommandName, out var overrideCmd))
            {
                category.SubCommands[i] = overrideCmd;
            }
        }

        // Find all learned actions that match this category's name (e.g. "Magic", "Skills")
        // or you could add a specific "CategoryTag" field to BattleCategory to match against ActionData.Category.
        var matchingActions = LearnedActions.OfType<ActionData>().Where(a => a.Category == category.CommandName).ToList();
        
        // Add them to the category's sub-commands
        foreach (var action in matchingActions)
        {
            // Check for override
            BattleCommand effectiveAction = action;
            if (_commandOverrides.TryGetValue(action.CommandName, out var overrideCmd))
            {
                effectiveAction = overrideCmd;
            }

            // Avoid duplicates if they were explicitly added in the editor
            if (!category.SubCommands.Contains(effectiveAction))
            {
                category.SubCommands.Add(effectiveAction);
            }
        }
    }
}
