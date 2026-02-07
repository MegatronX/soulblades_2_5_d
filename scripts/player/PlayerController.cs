using Godot;

/// <summary>
/// A component that handles player input and movement for a character.
/// This replaces the inheritance-based PlayerCharacter class.
/// Automatically sets up the appropriate InputProvider based on network authority.
/// </summary>
public partial class PlayerController : Node
{
    public const string DefaultName = "PlayerController";

    public IInputProvider InputProvider => _inputProvider;
    private IInputProvider _inputProvider;

    [Export]
    public float Speed = 5.0f;
    [Export]
    public float JumpVelocity = 4.5f;

    private int _localInputIndex = 0;
    
    /// <summary>
    /// Determines which local input set to use (0 = default, 1 = p1_, 2 = p2_, etc.).
    /// Useful for local split-screen where multiple characters are controlled by different devices.
    /// </summary>
    [Export]
    public int LocalInputIndex 
    { 
        get => _localInputIndex; 
        set { _localInputIndex = value; if (IsInsideTree()) InitializeInput(); }
    }

    /// <summary>
    /// Controls whether the character processes movement inputs.
    /// </summary>
    public bool IsMovementEnabled { get; set; } = true;

    private CharacterBody3D _characterBody;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private int _prevAuthority;

    public override void _Ready()
    {
        _characterBody = GetParentOrNull<CharacterBody3D>();
        if (_characterBody == null)
        {
            GD.PrintErr($"PlayerController attached to {GetParent().Name}, but it is not a CharacterBody3D.");
            SetPhysicsProcess(false);
            return;
        }

        // Initialize input based on current authority
        InitializeInput();
        _prevAuthority = GetMultiplayerAuthority();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Movement logic should only run on the server.
        if (!Multiplayer.IsServer()) return;
        if (_characterBody == null || _inputProvider == null) return;
        if (!IsMovementEnabled) return;

        ProcessMovement(delta);
    }

    /// <summary>
    /// Called by NetworkPlayerManager when the parent character's authority changes.
    /// </summary>
    public void OnAuthorityChanged()
    {
        _prevAuthority = GetMultiplayerAuthority();
        InitializeInput();
    }

    private void InitializeInput()
    {
        // Remove existing providers if we are switching modes
        if (_inputProvider is Node existingNode)
        {
            existingNode.QueueFree();
            _inputProvider = null;
        }

        if (IsMultiplayerAuthority())
        {
            // I have authority (Client owner or Server host), use local input.
            // We name it "InputProvider" regardless of type so RPC paths match on client and server.
            var provider = new LocalInputProvider { Name = "InputProvider", InputIndex = LocalInputIndex };
            AddChild(provider);
            _inputProvider = provider;
        }
        else
        {
            // I don't have authority, listen to network input.
            var provider = new NetworkInputProvider { Name = "InputProvider" };
            AddChild(provider);
            _inputProvider = provider;
        }
    }

    private void ProcessMovement(double delta)
    {
        Vector3 velocity = _characterBody.Velocity;
        
        // Add the gravity.
        if (!_characterBody.IsOnFloor())
            velocity.Y -= _gravity * (float)delta;

        // Handle Jump.
        if (_inputProvider.IsActionJustPressed(GameInputAction.Jump) && _characterBody.IsOnFloor())
            velocity.Y = JumpVelocity;

        Vector2 inputDir = _inputProvider.GetMoveVector();
        // Note: -inputDir.Y for standard 3D forward
        Vector3 direction = (_characterBody.Transform.Basis * new Vector3(inputDir.X, 0, -inputDir.Y)).Normalized(); 
        
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(_characterBody.Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(_characterBody.Velocity.Z, 0, Speed);
        }

        _characterBody.Velocity = velocity;
        _characterBody.MoveAndSlide();
    }
}
