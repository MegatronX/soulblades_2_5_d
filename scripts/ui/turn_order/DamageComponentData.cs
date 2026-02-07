using Godot;

/// <summary>
/// A component that adds damage properties to an action.
/// </summary>
[GlobalClass]
public partial class DamageComponentData : ActionComponentData
{
    [Export]
    public int Power { get; private set; } = 10;

    [Export(PropertyHint.Range, "0,100,1")]
    public int Accuracy { get; private set; } = 95;

    [Export]
    public Godot.Collections.Dictionary<ElementType, float> ElementalWeights { get; private set; } = new();
}