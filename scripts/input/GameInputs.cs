using Godot;
using System;

/// <summary>
/// A static class to hold StringName constants for all input actions.
/// This avoids using "magic strings" and provides compile-time safety and autocompletion.
/// The names must match the actions defined in Project -> Project Settings -> Input Map.
/// </summary>
public static class GameInputs
{
    public static readonly StringName MoveLeft = "move_left";
    public static readonly StringName MoveRight = "move_right";
    public static readonly StringName MoveUp = "move_up";
    public static readonly StringName MoveDown = "move_down";
    public static readonly StringName Jump = "jump";

    public static readonly StringName AuxLeft = "aux_left";
    public static readonly StringName AuxRight = "aux_right";
    

    /// <summary>
    /// Resolves the InputMap string name for a given GameInputAction and player index.
    /// </summary>
    public static string GetActionName(GameInputAction action, int inputIndex = 0)
    {
        string prefix = inputIndex > 0 ? $"p{inputIndex}_" : "";

        return action switch
        {
            GameInputAction.Up => prefix + MoveUp,
            GameInputAction.Down => prefix + MoveDown,
            GameInputAction.Left => prefix + MoveLeft,
            GameInputAction.Right => prefix + MoveRight,
            GameInputAction.Jump => prefix + Jump,
            GameInputAction.AuxLeft => prefix + AuxLeft,
            GameInputAction.AuxRight => prefix + AuxRight,
            GameInputAction.Attack => prefix + "attack",
            GameInputAction.Interact => prefix + "interact",
            GameInputAction.OpenMenu => prefix + "menu",
            // Add other cases as needed to match LocalInputProvider logic
            _ => prefix + action.ToString().ToLower()
        };
    }
}