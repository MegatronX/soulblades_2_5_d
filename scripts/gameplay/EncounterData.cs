using Godot;

/// <summary>
/// A Resource that pairs an EnemyParty with its spawn probability weight.
/// A higher weight makes the encounter more common relative to others on the same map.
/// </summary>
[GlobalClass]
public partial class EncounterData : Resource
{
    [Export]
    public EnemyParty Party { get; private set; }

    [Export(PropertyHint.Range, "1,100,1")]
    public int SpawnWeight { get; private set; } = 50;

    [Export]
    public BattleMusicData SpecificMusicTrack { get; private set; }

    [Export]
    public BattleMusicData PostBattleMusicTrack { get; private set; }

    [Export]
    public bool AllowRetry { get; private set; } = true;

    [Export]
    public bool IsScriptedLoss { get; private set; } = false;

    [Export]
    public bool SplitExperienceAcrossParty { get; private set; } = true;

    [Export]
    public bool SplitApAcrossParty { get; private set; } = true;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float KillingBlowExpBonusPercent { get; private set; } = 5f;
}
