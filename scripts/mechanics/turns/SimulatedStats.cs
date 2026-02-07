using System.Collections.Generic;

/// <summary>
/// A lightweight, non-Node class for holding a snapshot of a character's stats
/// for simulation purposes, such as turn order previews.
/// </summary>
public class SimulatedStats
{
    private readonly Dictionary<StatType, int> _statValues;

    /// <summary>
    /// Creates a simulated stats object by copying values from a real StatsComponent.
    /// </summary>
    public SimulatedStats(StatsComponent sourceStats)
    {
        // In a real implementation, you would deep copy the stat values.
        // For this example, we assume GetStatDictionary() returns a copy.
        // If it returns a direct reference, you must manually create a new dictionary.
        _statValues = new Dictionary<StatType, int>(sourceStats.GetStatDictionary());
    }
    
    /// <summary>
    /// Creates a simulated stats object by copying values from another SimulatedStats object.
    /// This is crucial for creating new simulation contexts from existing ones.
    /// </summary>
    public SimulatedStats(SimulatedStats sourceSimStats)
    {
        _statValues = new Dictionary<StatType, int>(sourceSimStats._statValues);
    }

    /// <summary>
    /// Gets the value of a specific stat.
    /// </summary>
    public float GetStatValue(StatType stat)
    {
        return _statValues.GetValueOrDefault(stat, 0);
    }

    /// <summary>
    /// Applies a temporary modification to a stat for simulation.
    /// This is where you would simulate the effects of a status effect.
    /// </summary>
    /// <param name="stat">The stat to modify.</param>
    /// <param name="modifier">The value to add (can be negative).</param>
    public void ApplyModifier(StatType stat, int modifier, float multiplier = 1f)
    {
        _statValues[stat] = (int)((_statValues.GetValueOrDefault(stat, 0) + modifier) * multiplier);
    }
}