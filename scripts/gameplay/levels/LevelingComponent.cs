using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks experience and level progression for a character.
/// </summary>
[GlobalClass]
public partial class LevelingComponent : Node
{
    public const string NodeName = "LevelingComponent";

    [Signal]
    public delegate void ExperienceChangedEventHandler(int newExperience);

    [Signal]
    public delegate void LevelUpEventHandler(int oldLevel, int newLevel);

    [Signal]
    public delegate void StatIncreasedEventHandler(long statType, int oldValue, int newValue);

    [Signal]
    public delegate void AbilityLearnedEventHandler(Ability ability);

    [Signal]
    public delegate void ActionLearnedEventHandler(ActionData action);

    [Export]
    public LevelProgression Progression { get; set; }

    [Export]
    public Godot.Collections.Array<StatGrowthRule> StatGrowthRules { get; set; } = new();

    [Export]
    public Godot.Collections.Array<LevelStatIncrementEntry> LevelStatIncrements { get; set; } = new();

    [Export]
    public Godot.Collections.Array<LevelRewardEntry> LevelRewards { get; set; } = new();

    [Export(PropertyHint.Range, "1,999,1")]
    public int StartingLevel { get; set; } = 1;

    [Export(PropertyHint.Range, "0,999999999,1")]
    public int StartingExperience { get; set; } = 0;

    public int CurrentLevel { get; private set; }
    public int CurrentExperience { get; private set; }

    private StatsComponent _stats;
    private AbilityManager _abilityManager;
    private ActionManager _actionManager;
    private bool _initialized;

    public override void _Ready()
    {
        _stats = GetParent().GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        _abilityManager = GetParent().GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
        _actionManager = GetParent().GetNodeOrNull<ActionManager>(ActionManager.DefaultName);

        CallDeferred(nameof(Initialize));
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        CurrentLevel = Mathf.Max(1, StartingLevel);
        CurrentExperience = Mathf.Max(0, StartingExperience);

        ApplyLevelStats();
        ApplyRewardsUpToLevel(CurrentLevel);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        CurrentExperience += amount;
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience);
        CheckForLevelUps();
    }

    private void CheckForLevelUps()
    {
        int maxLevel = Progression?.MaxLevel ?? 99;
        while (CurrentLevel < maxLevel)
        {
            int required = Progression?.GetTotalExpForLevel(CurrentLevel + 1) ?? int.MaxValue;
            if (CurrentExperience < required) break;
            PerformLevelUp();
        }
    }

    private void PerformLevelUp()
    {
        int oldLevel = CurrentLevel;
        CurrentLevel++;

        ApplyLevelStats();
        ApplyRewardsForLevel(CurrentLevel);

        EmitSignal(SignalName.LevelUp, oldLevel, CurrentLevel);
    }

    private void ApplyLevelStats()
    {
        if (_stats == null) return;
        if (StatGrowthRules.Count == 0 && LevelStatIncrements.Count == 0) return;

        var before = CaptureStatValues();

        _stats.RemoveAllModifiersFromSource(this);

        var totalBonuses = new Dictionary<StatType, int>();

        foreach (var rule in StatGrowthRules)
        {
            if (rule == null) continue;
            int baseValue = _stats.GetBaseStatValue(rule.Stat);
            int bonus = rule.GetAdditiveBonus(CurrentLevel, baseValue);
            if (bonus == 0) continue;
            totalBonuses[rule.Stat] = totalBonuses.GetValueOrDefault(rule.Stat, 0) + bonus;
        }

        foreach (var entry in LevelStatIncrements)
        {
            if (entry == null || entry.Level > CurrentLevel) continue;
            foreach (var inc in entry.Increments)
            {
                if (inc == null || inc.Amount == 0) continue;
                totalBonuses[inc.Stat] = totalBonuses.GetValueOrDefault(inc.Stat, 0) + inc.Amount;
            }
        }

        foreach (var kvp in totalBonuses)
        {
            var mod = new StatModifier(kvp.Key, kvp.Value, ModifierType.Additive, this);
            _stats.AddModifier(mod);
        }

        var after = CaptureStatValues();
        foreach (var kvp in after)
        {
            int oldValue = before.GetValueOrDefault(kvp.Key, kvp.Value);
            if (kvp.Value > oldValue)
            {
                EmitSignal(SignalName.StatIncreased, (long)kvp.Key, oldValue, kvp.Value);
            }
        }
    }

    private Dictionary<StatType, int> CaptureStatValues()
    {
        var values = new Dictionary<StatType, int>();
        foreach (var rule in StatGrowthRules)
        {
            if (rule == null) continue;
            values[rule.Stat] = _stats.GetStatValue(rule.Stat);
        }
        foreach (var entry in LevelStatIncrements)
        {
            if (entry == null) continue;
            foreach (var inc in entry.Increments)
            {
                if (inc == null) continue;
                values[inc.Stat] = _stats.GetStatValue(inc.Stat);
            }
        }
        return values;
    }

    private void ApplyRewardsUpToLevel(int level)
    {
        foreach (var entry in LevelRewards.Where(e => e != null && e.Level <= level))
        {
            ApplyEntry(entry);
        }
    }

    private void ApplyRewardsForLevel(int level)
    {
        foreach (var entry in LevelRewards.Where(e => e != null && e.Level == level))
        {
            ApplyEntry(entry);
        }
    }

    private void ApplyEntry(LevelRewardEntry entry)
    {
        if (entry == null) return;

        if (_abilityManager != null && entry.Abilities != null)
        {
            foreach (var ability in entry.Abilities)
            {
                if (ability == null) continue;
                if (_abilityManager.LearnAbility(ability))
                {
                    EmitSignal(SignalName.AbilityLearned, ability);
                }
            }
        }

        if (_actionManager != null && entry.Actions != null)
        {
            foreach (var action in entry.Actions)
            {
                if (action == null) continue;
                if (_actionManager.LearnAction(action))
                {
                    EmitSignal(SignalName.ActionLearned, action);
                }
            }
        }
    }
}
