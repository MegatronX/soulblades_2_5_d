using Godot;

/// <summary>
/// Data-driven rule mapping a combat event to Overflow gain.
/// </summary>
[GlobalClass]
public partial class OverflowGenerationRule : Resource
{
    [Export]
    public string RuleId { get; set; } = string.Empty;

    [Export]
    public OverflowTriggerType TriggerType { get; set; } = OverflowTriggerType.None;

    [Export]
    public int FlatAmount { get; set; } = 0;

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float PercentOfEventAmount { get; set; } = 0f;

    [Export]
    public int MinGain { get; set; } = 0;

    [Export]
    public int MaxGain { get; set; } = 0;

    [Export]
    public int PerRoundTriggerLimit { get; set; } = -1;

    [Export]
    public int PerRoundOverflowGainCap { get; set; } = -1;

    [Export]
    public Ability RequiredEquippedAbility { get; set; }

    [Export]
    public string GainReason { get; set; } = string.Empty;

    public string ResolveKey()
    {
        if (!string.IsNullOrEmpty(RuleId))
        {
            return RuleId;
        }

        if (!string.IsNullOrEmpty(ResourcePath))
        {
            return ResourcePath;
        }

        return $"{TriggerType}:{GetInstanceId()}";
    }

    public int ResolveGain(float eventAmount)
    {
        int gain = FlatAmount;
        if (PercentOfEventAmount > 0f && eventAmount != 0f)
        {
            gain += Mathf.RoundToInt(eventAmount * PercentOfEventAmount);
        }

        if (MinGain > 0)
        {
            gain = Mathf.Max(gain, MinGain);
        }

        if (MaxGain > 0)
        {
            gain = Mathf.Min(gain, MaxGain);
        }

        return gain;
    }
}
