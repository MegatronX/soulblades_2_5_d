using Godot;

/// <summary>
/// Tunables for Overflow economy and per-round constraints.
/// </summary>
[GlobalClass]
public partial class OverflowConfig : Resource
{
    [ExportGroup("Cap")]
    [Export]
    public OverflowProgressionTier ProgressionTier { get; private set; } = OverflowProgressionTier.Early;

    [Export]
    public int EarlyCap { get; private set; } = 1000;

    [Export]
    public int MidCap { get; private set; } = 4000;

    [Export]
    public int LateCap { get; private set; } = 12000;

    [Export]
    public bool ResetOnBattleStart { get; private set; } = true;

    [ExportGroup("Round Rules")]
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float EndOfRoundDecayPercent { get; private set; } = 0.10f;

    [Export]
    public int MaxUtilitiesPerRound { get; private set; } = 2;

    [Export]
    public int MaxFinishersPerRound { get; private set; } = 1;

    [ExportGroup("Baseline Timed Input")]
    [Export]
    public int PerfectTimedHitGain { get; private set; } = 100;

    [Export]
    public int PerfectTimedHitTriggerLimitPerRound { get; private set; } = 2;

    [Export]
    public int PerfectTimedGuardGain { get; private set; } = 150;

    [Export]
    public int PerfectTimedGuardTriggerLimitPerRound { get; private set; } = 2;

    [ExportGroup("Rule Sets")]
    [Export]
    public bool IncludeBaselineTimedRules { get; private set; } = true;

    [Export]
    public Godot.Collections.Array<OverflowGenerationRule> GenerationRules { get; private set; } = new();

    public int ResolveCap()
    {
        return ProgressionTier switch
        {
            OverflowProgressionTier.Mid => MidCap,
            OverflowProgressionTier.Late => LateCap,
            _ => EarlyCap,
        };
    }
}
