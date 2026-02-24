using Godot;

/// <summary>
/// Defines Overflow cost metadata for an action.
/// </summary>
[GlobalClass]
public partial class OverflowCostComponentData : ActionComponentData
{
    [Export]
    public int Cost { get; private set; } = 0;

    [Export]
    public OverflowSpendType SpendType { get; private set; } = OverflowSpendType.Utility;

    [Export]
    public bool IgnorePerRoundSpendLimits { get; private set; } = false;

    [Export]
    public string SpendReason { get; private set; } = string.Empty;
}
