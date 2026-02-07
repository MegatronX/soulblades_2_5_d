using Godot;
using System;

/// <summary>
/// A runtime menu that allows players to rebind keys for GameInputActions.
/// Supports dynamic player indices (p1_, p2_) via InputIndex.
/// </summary>
public partial class InputRemappingMenu : Control
{
    [Export] 
    public int InputIndex { get; set; } = 0;

    [Export] 
    private VBoxContainer _actionListContainer;

    private GameInputAction? _actionToRebind = null;
    private Button _activeRebindButton = null;

    public override void _Ready()
    {
        if (_actionListContainer == null)
        {
            GD.PrintErr("InputRemappingMenu: ActionListContainer is not assigned.");
            return;
        }

        BuildRemappingList();
    }

    public override void _Input(InputEvent @event)
    {
        // If we are not currently rebinding, ignore input.
        if (_actionToRebind == null) return;

        // We only care about Key or JoypadButton events (ignoring mouse movement, etc.)
        if (@event is InputEventKey || @event is InputEventJoypadButton)
        {
            // Consume the event so it doesn't trigger game logic
            GetViewport().SetInputAsHandled();
            
            PerformRebind(_actionToRebind.Value, @event);
            
            // Reset state
            _actionToRebind = null;
            _activeRebindButton.Disabled = false;
            _activeRebindButton = null;
        }
    }

    private void BuildRemappingList()
    {
        // Clear existing children
        foreach (Node child in _actionListContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (GameInputAction action in Enum.GetValues(typeof(GameInputAction)))
        {
            var hbox = new HBoxContainer();
            
            // Label for the action name
            var label = new Label();
            label.Text = action.ToString();
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            hbox.AddChild(label);

            // Button showing the current key
            var button = new Button();
            button.Text = GetCurrentKeyName(action);
            button.CustomMinimumSize = new Vector2(100, 0);
            
            // Capture variables for the closure
            var currentAction = action; 
            button.Pressed += () => StartRebinding(currentAction, button);
            
            hbox.AddChild(button);
            _actionListContainer.AddChild(hbox);
        }
    }

    private void StartRebinding(GameInputAction action, Button button)
    {
        _actionToRebind = action;
        _activeRebindButton = button;
        
        button.Text = "Press any key...";
        button.Disabled = true; // Disable to prevent double-clicks while waiting
    }

    private void PerformRebind(GameInputAction action, InputEvent newEvent)
    {
        string actionName = GameInputs.GetActionName(action, InputIndex);

        // Ensure the action exists in the InputMap (important for dynamic p1_ actions)
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        // Remove old events to avoid duplicates (or you could keep them for alternate keys)
        InputMap.ActionEraseEvents(actionName);
        
        // Add the new event
        InputMap.ActionAddEvent(actionName, newEvent);

        // Update the UI
        if (_activeRebindButton != null)
        {
            _activeRebindButton.Text = newEvent.AsText();
        }

        // Auto-save changes to disk
        InputPersistenceManager.Save();

        GD.Print($"Rebound {actionName} to {newEvent.AsText()}");
    }

    private string GetCurrentKeyName(GameInputAction action)
    {
        string actionName = GameInputs.GetActionName(action, InputIndex);
        if (!InputMap.HasAction(actionName)) return "None";

        var events = InputMap.ActionGetEvents(actionName);
        return events.Count > 0 ? events[0].AsText() : "None";
    }
}
