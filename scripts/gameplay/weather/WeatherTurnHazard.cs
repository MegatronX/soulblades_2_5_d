using Godot;

/// <summary>
/// Battle-only periodic hazard payload used by WeatherSystem.
/// Triggers from turn progression and applies lightweight elemental damage.
/// </summary>
[GlobalClass]
public partial class WeatherTurnHazard : Resource
{
    [Export]
    public string HazardName { get; private set; } = "Weather Hazard";

    [Export]
    public bool Enabled { get; private set; } = true;

    [Export(PropertyHint.Range, "1,20,1")]
    public int TriggerEveryTurnStarts { get; private set; } = 1;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float TriggerChancePercent { get; private set; } = 100f;

    [Export]
    public bool ApplyToAllCombatants { get; private set; } = false;

    [Export(PropertyHint.Range, "1,8,1")]
    public int RandomTargetCount { get; private set; } = 1;

    [Export]
    public bool CanTargetActiveTurnOwner { get; private set; } = true;

    [Export]
    public ElementType Element { get; private set; } = ElementType.None;

    [Export(PropertyHint.Range, "0,9999,1")]
    public int FlatDamageMin { get; private set; } = 0;

    [Export(PropertyHint.Range, "0,9999,1")]
    public int FlatDamageMax { get; private set; } = 0;

    [Export(PropertyHint.Range, "0,1,0.001")]
    public float PercentOfMaxHpDamage { get; private set; } = 0f;

    [Export]
    public PackedScene ImpactVfx { get; private set; }

    [Export]
    public Vector3 ImpactVfxOffset { get; private set; } = Vector3.Zero;

    [Export]
    public AudioStream ImpactSfx { get; private set; }

    [Export]
    public float ImpactSfxVolumeDb { get; private set; } = -3f;

    [Export(PropertyHint.Range, "0.5,2.0,0.01")]
    public float ImpactPitchMin { get; private set; } = 0.95f;

    [Export(PropertyHint.Range, "0.5,2.0,0.01")]
    public float ImpactPitchMax { get; private set; } = 1.05f;

    [Export]
    public string CombatLogMessage { get; private set; } = string.Empty;
}
