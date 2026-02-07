using Godot;

public interface IInputProvider
{
    // Specialized composite action
    Vector2 GetMoveVector();
    
    // Generic "is held down" action
    bool IsActionPressed(GameInputAction action);
    
    // Generic "was just pressed" action
    bool IsActionJustPressed(GameInputAction action);
}