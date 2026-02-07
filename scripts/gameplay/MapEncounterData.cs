using Godot;

/// <summary>
/// A Resource that defines the complete encounter table for a specific map,
/// including all possible enemy parties and the overall encounter rate.
/// </summary>
[GlobalClass]
public partial class MapEncounterData : Resource
{
    [Export]
    public Godot.Collections.Array<EncounterData> PossibleEncounters { get; private set; }

    [Export]
    public Godot.Collections.Array<BattleMusicData> BattleMusicTracks { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<BattleMusicData> PostBattleMusicTracks { get; private set; } = new();

    [Export(PropertyHint.Range, "0,100,1")]
    public int EncounterRatePercent { get; private set; } = 10; // e.g., a 10% chance per step/second.

    [Export]
    public BattleEnvironmentProfile EnvironmentProfile { get; private set; }
}
