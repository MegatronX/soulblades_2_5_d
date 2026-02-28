using Godot;

/// <summary>
/// Authoring profile for ambient exploration props (fireflies, wildlife audio nodes, etc.).
/// </summary>
[GlobalClass]
public partial class AmbientPropProfile : Resource
{
    [Export]
    public string ProfileName { get; private set; } = "AmbientProps";

    [Export]
    public Godot.Collections.Array<PackedScene> PropScenes { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<AmbientPropSpawnEntry> SpawnEntries { get; private set; } = new();

    [Export(PropertyHint.Range, "0,64,1")]
    public int InitialSpawnCount { get; private set; } = 6;

    [Export(PropertyHint.Range, "0,128,1")]
    public int MaxActiveProps { get; private set; } = 12;

    [Export(PropertyHint.Range, "0.1,30,0.1")]
    public float SpawnIntervalSeconds { get; private set; } = 2.5f;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float SpawnRadius { get; private set; } = 10f;

    [Export(PropertyHint.Range, "0,150,0.1")]
    public float DespawnRadius { get; private set; } = 35f;

    [Export(PropertyHint.Range, "-2,10,0.1")]
    public float VerticalJitter { get; private set; } = 1.5f;
}
