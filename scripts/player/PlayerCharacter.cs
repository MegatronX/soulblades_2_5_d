using Godot;


/// <summary>
/// Main script for the player character.
/// It handles movement and game logic.
/// Movement is processed ONLY on the server, and the state is replicated to clients.
/// </summary>
public partial class PlayerCharacter : BaseCharacter
{
    private IInputProvider _inputProvider;

    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        base._Ready();

        // The peer with authority over this character determines the input source.
        if (IsMultiplayerAuthority())
        {
            // I have authority, so I'll use the local input provider.
            _inputProvider = GetNode<LocalInputProvider>("LocalInputProvider");
        }
        else
        {
            // I don't have authority. On the server, this will be the NetworkInputProvider.
            // On other clients, this node might not exist, but it won't be used anyway.
            _inputProvider = GetNode<NetworkInputProvider>("NetworkInputProvider");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Movement logic should only run on the server.
        if (Multiplayer.IsServer())
        {
            ProcessMovement(delta);
        }
    }

    private void ProcessMovement(double delta)
    {
        Vector3 velocity = Velocity;
        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Handle Jump.
        if (_inputProvider.IsActionJustPressed(GameInputAction.Jump))
            velocity.Y = JumpVelocity;

        Vector2 inputDir = _inputProvider.GetMoveVector();
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, -inputDir.Y)).Normalized(); // Note: -inputDir.Y for standard 3D forward
        velocity.X = direction.X * Speed;
        velocity.Z = direction.Z * Speed;
        Velocity = velocity;
        MoveAndSlide();
    }
}