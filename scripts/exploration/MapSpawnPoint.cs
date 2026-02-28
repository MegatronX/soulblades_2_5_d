using Godot;

/// <summary>
/// Identifies a spawn anchor in an exploration map.
/// </summary>
[GlobalClass]
public partial class MapSpawnPoint : Node3D
{
    [Export]
    public string SpawnId { get; private set; } = "default";

    public override void _Ready()
    {
        AddToGroup(ExplorationGroups.MapSpawnPoints);
    }
}
