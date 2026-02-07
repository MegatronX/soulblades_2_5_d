using Godot;

public enum ModifierType
{
    Additive,       // Adds a flat value to the stat (e.g., +10 Attack)
    Multiplicative  // Multiplies the stat by a value (e.g., Speed * 1.5)
}

/// <summary>
/// A simple resource to define a direct modification to a single stat.
/// Used by equippable items.
/// </summary>
[GlobalClass]
public partial class StatModifier : Resource
{
    [Export]
    public StatType StatToModify { get; private set; }

    [Export]
    public ModifierType Type { get; private set; } = ModifierType.Additive;

    [Export]
    public float Value { get; private set; }

    public object Source { get; set; }

    public StatModifier(StatType stat, float value, ModifierType modtype, object source)
    {
        StatToModify = stat;
        Value = value;
        Type = modtype;
        Source = source;
    }
}
