using Godot;

/// <summary>
/// Defines a single stat multiplier entry for stat-altering status effects.
/// </summary>
[GlobalClass]
public partial class StatMultiplierEntry : Resource
{
    [Export]
    public StatType Stat { get; private set; }

    [Export]
    public float Multiplier { get; private set; } = 1.0f;
}
