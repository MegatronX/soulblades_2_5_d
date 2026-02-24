using Godot;

/// <summary>
/// A normalized Overflow generation event consumed by OverflowSystem rules.
/// </summary>
[GlobalClass]
public partial class OverflowEventData : RefCounted
{
    [Export]
    public OverflowTriggerType TriggerType { get; set; } = OverflowTriggerType.None;

    public Node Source { get; set; }

    public Node Target { get; set; }

    [Export]
    public float Amount { get; set; } = 0f;

    [Export]
    public bool IsUnique { get; set; } = false;

    [Export]
    public string Reason { get; set; } = string.Empty;
}
