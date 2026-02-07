using Godot;

/// <summary>
/// A simple node that ensures its children only exist on the server.
/// </summary>
public partial class Server : Node
{
    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            QueueFree();
        }
    }
}