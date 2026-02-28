using Godot;

/// <summary>
/// Lightweight follow camera for exploration maps.
/// </summary>
[GlobalClass]
public partial class ExplorationCameraController : Node3D
{
    [Export]
    public NodePath TargetPath { get; private set; }

    [Export]
    public Vector3 FollowOffset { get; private set; } = new Vector3(0f, 7.5f, 9.5f);

    [Export(PropertyHint.Range, "0,25,0.01")]
    public float FollowLerpSpeed { get; private set; } = 8f;

    [Export]
    public bool SnapOnReady { get; private set; } = true;

    [Export]
    public bool LookAtTarget { get; private set; } = true;

    [Export]
    public Vector3 LookAtOffset { get; private set; } = new Vector3(0f, 1.2f, 0f);

    private Node3D _target;

    public override void _Ready()
    {
        _target = GetNodeOrNull<Node3D>(TargetPath);
        if (_target == null)
        {
            _target = GetTree()?.GetFirstNodeInGroup(GameGroups.PlayerCharacters) as Node3D;
        }

        if (_target != null && SnapOnReady)
        {
            GlobalPosition = _target.GlobalPosition + FollowOffset;
            ApplyLook();
        }
    }

    public override void _Process(double delta)
    {
        if (_target == null || !GodotObject.IsInstanceValid(_target))
        {
            _target = GetTree()?.GetFirstNodeInGroup(GameGroups.PlayerCharacters) as Node3D;
            if (_target == null) return;
        }

        Vector3 desired = _target.GlobalPosition + FollowOffset;
        float weight = Mathf.Clamp(FollowLerpSpeed * (float)delta, 0f, 1f);
        GlobalPosition = GlobalPosition.Lerp(desired, weight);

        ApplyLook();
    }

    private void ApplyLook()
    {
        if (!LookAtTarget || _target == null) return;
        LookAt(_target.GlobalPosition + LookAtOffset, Vector3.Up);
    }
}
