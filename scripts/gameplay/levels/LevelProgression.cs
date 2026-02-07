using Godot;

public enum ExpRequirementMode
{
    CurveTotal,
    TableTotal
}

/// <summary>
/// Defines how much experience is required for each level.
/// </summary>
[GlobalClass]
public partial class LevelProgression : Resource
{
    [Export]
    public int MaxLevel { get; set; } = 99;

    [Export]
    public ExpRequirementMode ExpMode { get; set; } = ExpRequirementMode.CurveTotal;

    [Export]
    public Curve ExpCurve { get; set; }

    [Export]
    public Godot.Collections.Array<int> ExpTable { get; set; } = new();

    public int GetTotalExpForLevel(int level)
    {
        if (level <= 1) return 0;

        level = Mathf.Clamp(level, 1, MaxLevel);

        if (ExpMode == ExpRequirementMode.TableTotal && ExpTable.Count > 0)
        {
            int index = Mathf.Clamp(level - 1, 0, ExpTable.Count - 1);
            return ExpTable[index];
        }

        if (ExpMode == ExpRequirementMode.CurveTotal && ExpCurve != null)
        {
            return Mathf.RoundToInt(ExpCurve.SampleBaked(level));
        }

        // Fallback: simple quadratic curve
        return level * level * 10;
    }
}
