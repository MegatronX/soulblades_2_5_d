using Godot;
using System.Collections.Generic;
using System;

public partial class LocalInputProvider : Node, IInputProvider
{
    [Export]
    public int InputIndex { get; set; } = 0;
    
    // If InputIndex > 0, we prefix actions (e.g. "p1_jump"). 
    // If 0, we use standard actions ("jump") to support default Input Map settings.

    // This dictionary maps our abstract enum to Godot's strings
    private Dictionary<GameInputAction, string> _actionMap = new();

    // Still need these for GetVector
    private string _moveUp;
    private string _moveDown;
    private string _moveLeft;
    private string _moveRight;

    public override void _Ready()
    {
        // Use the centralized helper to ensure consistency with the Remapping Menu
        _moveUp = GameInputs.GetActionName(GameInputAction.Up, InputIndex);
        _moveDown = GameInputs.GetActionName(GameInputAction.Down, InputIndex);
        _moveLeft = GameInputs.GetActionName(GameInputAction.Left, InputIndex);
        _moveRight = GameInputs.GetActionName(GameInputAction.Right, InputIndex);

        // Setup the dictionary
        foreach (GameInputAction action in Enum.GetValues(typeof(GameInputAction)))
        {
            _actionMap[action] = GameInputs.GetActionName(action, InputIndex);
        }
    }

    public Vector2 GetMoveVector()
    {
        // Verify all directional actions exist to prevent GetVector from throwing an error.
        if (!InputMap.HasAction(_moveLeft) || !InputMap.HasAction(_moveRight) || 
            !InputMap.HasAction(_moveUp) || !InputMap.HasAction(_moveDown))
        {
            return Vector2.Zero;
        }
        return Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);
    }

    public bool IsActionPressed(GameInputAction action)
    {
        if (_actionMap.TryGetValue(action, out string actionName) && InputMap.HasAction(actionName))
        {
            return Input.IsActionPressed(actionName);
        }
        return false;
    }

    public bool IsActionJustPressed(GameInputAction action)
    {
        if (_actionMap.TryGetValue(action, out string actionName) && InputMap.HasAction(actionName))
        {
            return Input.IsActionJustPressed(actionName);
        }
        return false;
    }
}