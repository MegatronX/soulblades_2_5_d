// NetworkInputProvider.cs
using Godot;
using System.Collections.Generic;

public partial class NetworkInputProvider : Node, IInputProvider
{
    // A dictionary to hold the state of all actions
    // This would be populated by your network logic
    private Dictionary<GameInputAction, bool> _pressedState = new();
    private Dictionary<GameInputAction, bool> _justPressedState = new();
    
    [Export]
    public Vector2 SyncedMoveVector { get; set; } = Vector2.Zero;

    public Vector2 GetMoveVector() => SyncedMoveVector;

    public bool IsActionPressed(GameInputAction action)
    {
        return _pressedState.GetValueOrDefault(action, false);
    }

    public bool IsActionJustPressed(GameInputAction action)
    {
        // "Just pressed" events are consumed on read
        if (_justPressedState.GetValueOrDefault(action, false))
        {
            _justPressedState[action] = false; // Consume it
            return true;
        }
        return false;
    }

    // You would have an RPC method to update these states
    [Rpc]
    public void UpdateNetworkState(Vector2 moveVector, Godot.Collections.Dictionary pressed, Godot.Collections.Dictionary justPressed)
    {
        // Update the synced move vector
        SyncedMoveVector = moveVector;

        // Clear the old state
        _pressedState.Clear();
        _justPressedState.Clear();

        // Convert from Godot Dictionary to a standard C# Dictionary
        foreach (var (key, value) in pressed)
        {
            _pressedState[(GameInputAction)key.AsInt64()] = value.AsBool();
        }

        foreach (var (key, value) in justPressed)
        {
            _justPressedState[(GameInputAction)key.AsInt64()] = value.AsBool();
        }
    }
}