using Godot;

/// <summary>
/// A Resource that defines a single character stat, like Health or Strength.
/// By using [GlobalClass], we can create and save these as .tres files in the editor.
/// </summary>
[GlobalClass]
public partial class Stat : Resource
{
    [Export]
    public StatType Type { get; private set; }

    [Export]
    public int BaseValue { get; private set; }
}