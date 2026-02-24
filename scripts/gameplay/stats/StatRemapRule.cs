using Godot;

/// <summary>
/// Data definition for remapping one requested stat to resolve from another stat.
/// Example: Source=Magic, Target=Strength means Strength lookups use Magic.
/// </summary>
[GlobalClass]
public partial class StatRemapRule : Resource
{
    [Export]
    public StatType SourceStat { get; private set; } = StatType.Strength;

    [Export]
    public StatType TargetStat { get; private set; } = StatType.Strength;

    [Export]
    public int RemapCount { get; private set; } = 1;
}
