using Godot;

/// <summary>
/// Authoring profile for ambient exploration props (fireflies, wildlife audio nodes, etc.).
/// </summary>
[GlobalClass]
public partial class AmbientPropProfile : Resource
{
    [ExportCategory("Ambient Prop Profile")]
    [Export]
    public string ProfileName { get; private set; } = "AmbientProps";

    /// <summary>
    /// Single source of truth for ambient spawning configuration.
    /// Each entry controls scene, weight, cooldowns, caps, and optional motion/lifetime.
    /// </summary>
    [Export]
    public Godot.Collections.Array<AmbientPropSpawnEntry> SpawnEntries { get; private set; } = new();

    [ExportGroup("Spawn Core (counts and cadence)")]
    [Export(PropertyHint.Range, "0,64,1,suffix:props")]
    public int InitialSpawnCount { get; private set; } = 6;

    [Export(PropertyHint.Range, "0,128,1,suffix:props")]
    public int MaxActiveProps { get; private set; } = 12;

    [Export(PropertyHint.Range, "0.1,30,0.1,suffix:s")]
    public float SpawnIntervalSeconds { get; private set; } = 2.5f;

    [ExportGroup("Spawn Space (where props can appear around follow target)")]
    [Export(PropertyHint.Range, "0,100,0.1,suffix:m")]
    public float SpawnRadius { get; private set; } = 10f;

    [Export(PropertyHint.Range, "0,150,0.1,suffix:m")]
    public float DespawnRadius { get; private set; } = 35f;

    [Export(PropertyHint.Range, "-2,10,0.1,suffix:m")]
    public float VerticalJitter { get; private set; } = 1.5f;

    [ExportGroup("Spawn Fade (smooth pop-in / pop-out)")]
    [Export]
    public bool EnableSpawnDespawnFade { get; private set; } = true;

    [Export(PropertyHint.Range, "0,5,0.01,suffix:s")]
    public float SpawnFadeSeconds { get; private set; } = 0.2f;

    [Export(PropertyHint.Range, "0,5,0.01,suffix:s")]
    public float DespawnFadeSeconds { get; private set; } = 0.35f;

    [ExportGroup("Spawn Visibility (camera-aware constraints)")]
    [Export]
    public bool SpawnOnlyInCameraView { get; private set; } = false;

    [Export]
    public NodePath SpawnCameraPath { get; private set; } = new NodePath();

    [Export(PropertyHint.Range, "0,0.4,0.01,suffix:viewport")]
    public float ViewportSpawnMargin { get; private set; } = 0.05f;

    [Export(PropertyHint.Range, "1,64,1,suffix:attempts")]
    public int SpawnPositionAttempts { get; private set; } = 12;
}
