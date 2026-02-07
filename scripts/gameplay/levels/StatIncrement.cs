using Godot;

/// <summary>
/// Defines a flat stat increment applied at a specific level.
/// </summary>
[GlobalClass]
public partial class StatIncrement : Resource
{
    [Export]
    public StatType Stat { get; set; }

    [Export(PropertyHint.Range, "-100000,100000,1")]
    public int Amount { get; set; } = 0;
}
