using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A component Node that manages a collection of stats for a character.
/// This node should be added as a child to any character that needs stats.
/// </summary>
public partial class StatsComponent : Node
{
    public const string NodeName = "StatsComponent";

    [Export]
    private BaseStats _baseStatsResource;

    // The character's foundational stats.
    private readonly Dictionary<StatType, int> _characterBaseStats = new();
    // A list of all active stat modifiers from equipment, status effects, etc.
    private readonly List<StatModifier> _statModifiers = new();

    // A cache for the final calculated stat values.
    private readonly Dictionary<StatType, int> _finalValues = new();
    private bool _isDirty = true; // Flag to recalculate stats only when needed.

    /// <summary>
    /// The character's current, volatile HP.
    /// </summary>
    public int CurrentHP { get; private set; }

    /// <summary>
    /// The character's current, volatile MP.
    /// </summary>
    public int CurrentMP { get; private set; }

    [Signal] public delegate void CurrentHPChangedEventHandler(int newHP, int maxHP);
    [Signal] public delegate void CurrentMPChangedEventHandler(int newMP, int maxMP);

    /// <summary>
    /// Emitted when any calculated stat (like Strength, Defense, Max HP, etc.) changes.
    /// </summary>
    [Signal] public delegate void StatValueChangedEventHandler(long statType, int newValue);

        [Signal]
    public delegate void HealthDepletedEventHandler();

    public override void _Ready()
    {
        InitializeStats();
    }

    /// <summary>
    /// Public method to set the BaseStats resource, primarily for use when creating
    /// characters dynamically in code (e.g., in tests or from network data).
    /// This also triggers the initial stat calculation.
    /// </summary>
    public void SetBaseStatsResource(BaseStats baseStats)
    {
        _baseStatsResource = baseStats;
        InitializeStats();
    }

    /// <summary>
    /// Gets the final, calculated value of a specific stat.
    /// </summary>
    public int GetStatValue(StatType statType)
    {
        if (_isDirty)
        {
            RecalculateAllStats();
        }
        return _finalValues.GetValueOrDefault(statType, 0);
    }

    public int GetStatValueWithoutModifiersFromSource(StatType statType, object source)
    {
        if (_isDirty)
        {
            RecalculateAllStats();
        }
        return (int)CalculateSingleStat(statType, _statModifiers.Where(m => m.StatToModify == statType && m.Source != source).ToList());
    }

    /// <summary>
    /// Adds a new modifier and marks the stats as needing recalculation.
    /// </summary>
    public void AddModifier(StatModifier modifier)
    {
        _statModifiers.Add(modifier);
        _isDirty = true;
    }

    /// <summary>
    /// Removes all modifiers originating from a specific source (e.g., unequipping a sword).
    /// </summary>
    public void RemoveAllModifiersFromSource(object source)
    {
        _statModifiers.RemoveAll(mod => mod.Source == source);
        _isDirty = true;
    }

    /// <summary>
    /// Modifies the CurrentHP by a given amount (positive for healing, negative for damage).
    /// Automatically clamps the value between 0 and the character's Max HP.
    /// </summary>
    public void ModifyCurrentHP(int amount)
    {
        int maxHP = GetStatValue(StatType.HP);
        int old = CurrentHP;
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, maxHP);
        EmitSignal(SignalName.CurrentHPChanged, CurrentHP, maxHP);
        if (CurrentHP <= 0 && old > 0)
            {
                EmitSignal(SignalName.HealthDepleted);
            }
    }

    /// <summary>
    /// Modifies the CurrentMP by a given amount.
    /// Automatically clamps the value between 0 and the character's Max MP.
    /// </summary>
    public void ModifyCurrentMP(int amount)
    {
        int maxMP = GetStatValue(StatType.MP);
        CurrentMP = Mathf.Clamp(CurrentMP + amount, 0, maxMP);
        EmitSignal(SignalName.CurrentMPChanged, CurrentMP, maxMP);
    }

    /// <summary>
    /// Your concern about predictive calculation is solved here.
    /// This method calculates the value of a stat with a potential new modifier
    /// without actually applying it to the character.
    /// </summary>
    /// <param name="statType">The stat to calculate.</param>
    /// <param name="hypotheticalModifier">The new modifier to consider.</param>
    /// <returns>The predicted final value of the stat.</returns>
    public int PredictStatValue(StatType statType, StatModifier hypotheticalModifier)
    {
        // Create a temporary list of modifiers including the new one.
        var hypotheticalModifiers = new List<StatModifier>(_statModifiers) { hypotheticalModifier };
        return (int)CalculateSingleStat(statType, hypotheticalModifiers);
    }

    private void InitializeStats()
    {
        if (_baseStatsResource == null) return;

        _characterBaseStats[StatType.HP] = _baseStatsResource.HP;
        _characterBaseStats[StatType.MP] = _baseStatsResource.MP;
        _characterBaseStats[StatType.Strength] = _baseStatsResource.Strength;
        _characterBaseStats[StatType.Defense] = _baseStatsResource.Defense;
        _characterBaseStats[StatType.Magic] = _baseStatsResource.Magic;
        _characterBaseStats[StatType.MagicDefense] = _baseStatsResource.MagicDefense;
        _characterBaseStats[StatType.Speed] = _baseStatsResource.Speed;
        _characterBaseStats[StatType.Evasion] = _baseStatsResource.Evasion;
        _characterBaseStats[StatType.MgEvasion] = _baseStatsResource.MgEvasion;
        _characterBaseStats[StatType.Accuracy] = _baseStatsResource.Accuracy;
        _characterBaseStats[StatType.MgAccuracy] = _baseStatsResource.MgAccuracy;
        _characterBaseStats[StatType.Luck] = _baseStatsResource.Luck;
        _characterBaseStats[StatType.AP] = _baseStatsResource.AP;

        // Perform an initial calculation to populate the final values.
        RecalculateAllStats();

        // Set current volatile stats to their maximums upon initialization.
        CurrentHP = GetStatValue(StatType.HP);
        CurrentMP = GetStatValue(StatType.MP);
    }

    private void RecalculateAllStats()
    {
        if (!_isDirty) return;

        // Create a temporary copy of the old values to detect changes.
        var oldFinalValues = new Dictionary<StatType, int>(_finalValues);
        _finalValues.Clear();

        foreach (StatType statType in _characterBaseStats.Keys)
        {
            int newValue = (int)CalculateSingleStat(statType, _statModifiers.Where(m => m.StatToModify == statType).ToList());
            _finalValues[statType] = newValue;

            // If the calculated value has changed, emit a signal.
            if (!oldFinalValues.TryGetValue(statType, out int oldValue) || oldValue != newValue)
            {
                EmitSignal(SignalName.StatValueChanged, (long)statType, newValue);
            }
        }
        _isDirty = false;

        // After recalculating, ensure current HP/MP don't exceed the new maximums.
        // This handles cases where a debuff might lower a character's Max HP.
        int oldHP = CurrentHP;
        int maxHP = _finalValues.GetValueOrDefault(StatType.HP, 0);
        CurrentHP = Mathf.Clamp(CurrentHP, 0, maxHP);
        if (oldHP != CurrentHP)
        {
            EmitSignal(SignalName.CurrentHPChanged, CurrentHP, maxHP);
        }

        int oldMP = CurrentMP;
        int maxMP = _finalValues.GetValueOrDefault(StatType.MP, 0);
        CurrentMP = Mathf.Clamp(CurrentMP, 0, maxMP);
        if (oldMP != CurrentMP)
        {
            EmitSignal(SignalName.CurrentMPChanged, CurrentMP, maxMP);
        }
    }

    private double CalculateSingleStat(StatType statType, List<StatModifier> modifiers)
    {
        double baseValue = _characterBaseStats.GetValueOrDefault(statType, 0);

        if (modifiers == null || modifiers.Count == 0)
        {
            return baseValue;
        }

        // 1. Apply all additive modifiers
        float totalAdditive = 0f;
        foreach (var mod in modifiers.Where(m => m.Type == ModifierType.Additive))
        {
            totalAdditive += mod.Value;
        }
        double valueAfterAdditives = baseValue + totalAdditive;

        // 2. Apply all multiplicative modifiers
        double totalMultiplier = 1.0;
        foreach (var mod in modifiers.Where(m => m.Type == ModifierType.Multiplicative))
        {
            totalMultiplier *= mod.Value;
        }

        // Final calculation
        double finalValue = valueAfterAdditives * totalMultiplier;
        // We use Math.Floor to always round down, which is common in JRPGs.
        return System.Math.Floor(finalValue);
    }

    public IReadOnlyDictionary<StatType, int> GetStatDictionary()
    {
        return _finalValues;
    }
}