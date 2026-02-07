using Godot;

public enum StatGrowthMode
{
    AddPerLevel,
    AbsoluteCurve,
    AbsoluteTable,
    IncrementTable
}

/// <summary>
/// Defines how a single stat grows with level.
/// </summary>
[GlobalClass]
public partial class StatGrowthRule : Resource
{
    [Export]
    public StatType Stat { get; set; }

    [Export]
    public StatGrowthMode Mode { get; set; } = StatGrowthMode.AddPerLevel;

    [Export(PropertyHint.Range, "0,100000,0.1")]
    public float AddPerLevel { get; set; } = 1.0f;

    [Export]
    public Curve AbsoluteCurve { get; set; }

    [Export]
    public Godot.Collections.Array<int> AbsoluteTable { get; set; } = new();

    [Export]
    public Godot.Collections.Array<int> IncrementTable { get; set; } = new();

    [Export]
    public Godot.Collections.Dictionary<int, int> IncrementMap { get; set; } = new();

    public int GetAdditiveBonus(int level, int baseValue)
    {
        level = Mathf.Max(1, level);

        switch (Mode)
        {
            case StatGrowthMode.AddPerLevel:
                return Mathf.RoundToInt(AddPerLevel * (level - 1));
            case StatGrowthMode.AbsoluteCurve:
                return GetAbsoluteFromCurve(level, baseValue) - baseValue;
            case StatGrowthMode.AbsoluteTable:
                return GetAbsoluteFromTable(level, baseValue) - baseValue;
            case StatGrowthMode.IncrementTable:
                return GetIncrementTotal(level);
            default:
                return 0;
        }
    }

    private int GetAbsoluteFromCurve(int level, int fallback)
    {
        if (AbsoluteCurve == null) return fallback;
        return Mathf.RoundToInt(AbsoluteCurve.SampleBaked(level));
    }

    private int GetAbsoluteFromTable(int level, int fallback)
    {
        if (AbsoluteTable.Count == 0) return fallback;
        int index = Mathf.Clamp(level - 1, 0, AbsoluteTable.Count - 1);
        return AbsoluteTable[index];
    }

    private int GetIncrementTotal(int level)
    {
        if (IncrementMap.Count > 0)
        {
            int total = 0;
            for (int lvl = 2; lvl <= level; lvl++)
            {
                if (IncrementMap.TryGetValue(lvl, out int inc))
                {
                    total += inc;
                }
            }
            return total;
        }

        if (IncrementTable.Count == 0) return 0;

        int tableTotal = 0;
        for (int lvl = 2; lvl <= level; lvl++)
        {
            int index = Mathf.Clamp(lvl - 1, 0, IncrementTable.Count - 1);
            tableTotal += IncrementTable[index];
        }
        return tableTotal;
    }
}
