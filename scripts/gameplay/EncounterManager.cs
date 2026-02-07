using Godot;
using System.Linq;

/// <summary>
/// A component placed on an overworld map to manage random encounters.
/// It uses a MapEncounterData resource to determine when and what to spawn.
/// </summary>
public partial class EncounterManager : Node
{
    [Export]
    private MapEncounterData _mapData;

    [Export(PropertyHint.File, "*.tscn")]
    private string _battleScenePath;

    private IRandomNumberGenerator _rng = new GodotRandomNumberGenerator();

    // This could be called every step the player takes, or on a timer.
    public void CheckForEncounter()
    {
        if (_mapData == null || _mapData.PossibleEncounters.Count == 0) return;

        int roll = _rng.RandRangeInt(1, 100);
        if (roll <= _mapData.EncounterRatePercent)
        {
            TriggerEncounter();
        }
    }

    /// <summary>
    /// Forces an encounter to start immediately, ignoring encounter rates.
    /// Useful for testing or scripted events.
    /// </summary>
    public void ForceEncounter()
    {
        if (_mapData == null || _mapData.PossibleEncounters.Count == 0)
        {
            GD.PrintErr("Cannot force encounter: MapData is missing or empty.");
            return;
        }
        TriggerEncounter();
    }

    private void TriggerEncounter()
    {
        var encounter = GetRandomEncounter();
        if (encounter != null)
        {
            var gameManager = GetNode<GameManager>(GameManager.Path);
            // Wrap the single party in a list for the new method signature.
            var enemyParties = new System.Collections.Generic.List<Godot.Collections.Array<PackedScene>> { encounter.Party.Members };
            
            // Determine music track
            BattleMusicData music = encounter.SpecificMusicTrack;
            if (music == null && _mapData.BattleMusicTracks.Count > 0)
            {
                // Weighted random selection
                int totalWeight = _mapData.BattleMusicTracks.Sum(t => t.Weight);
                int roll = _rng.RandRangeInt(0, totalWeight - 1);
                
                foreach (var track in _mapData.BattleMusicTracks)
                {
                    if (roll < track.Weight)
                    {
                        music = track;
                        break;
                    }
                    roll -= track.Weight;
                }
            }

            // For a standard random encounter, we have no allies and a normal formation.
            gameManager.InitiateBattle(enemyParties, null, _battleScenePath, BattleFormation.Normal, _mapData?.EnvironmentProfile, null, music);
        }
    }

    private EncounterData GetRandomEncounter()
    {
        int totalWeight = _mapData.PossibleEncounters.Sum(e => e.SpawnWeight);
        int randomWeight = _rng.RandRangeInt(0, totalWeight - 1);

        foreach (var encounter in _mapData.PossibleEncounters)
        {
            if (randomWeight < encounter.SpawnWeight)
            {
                return encounter;
            }
            randomWeight -= encounter.SpawnWeight;
        }

        return null; // Should not happen if weights are set up correctly.
    }
}