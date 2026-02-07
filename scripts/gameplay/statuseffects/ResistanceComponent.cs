using System.Collections.Generic;
using Godot;

/// <summary>
/// A component that holds a character's resistances to various status effects.
/// This allows for data-driven design of character immunities and weaknesses.
/// </summary>
[GlobalClass]
public partial class ResistanceComponent : Node
{
    /// <summary>
    /// A dictionary mapping a StatusEffect resource to a resistance percentage (0-100).
    /// A value of 100 means full immunity.
    /// This can be configured in the Godot Inspector.
    /// </summary>
    [Export]
    public Godot.Collections.Dictionary<string, int> Resistances { get; private set; } = new();

    /// <summary>
    /// Gets the resistance percentage for a given status effect.
    /// </summary>
    /// <returns>A value from 0 to 100, where 100 means immunity.</returns>
    public int GetResistance(StatusEffect effectData)
    {
        // Clamp the value to ensure it's always within the 0-100 range.
        return Mathf.Clamp(Resistances.GetValueOrDefault(effectData.EffectName, 0), 0, 100);
    }
}