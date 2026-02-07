using Godot;

public enum MultiTargetApproachStrategy
{
    None,    // Do not move if multiple targets
    Average, // Move to the center point of all targets
    First    // Move to the first target in the list
}

/// <summary>
/// Defines the visual presentation of an action, including animations, VFX, and timing.
/// </summary>
[GlobalClass]
public partial class VisualActionSettings : Resource
{
    [ExportGroup("Windup (Caster)")]
    [Export] public string WindupAnimation { get; set; } = "Cast";
    [Export] public bool WaitForWindupAnimation { get; set; } = true;
    [Export] public PackedScene WindupVfx { get; set; }
    [Export] public Vector3 WindupVfxOffset { get; set; } = Vector3.Zero;
    [Export] public bool ParentWindupVfx { get; set; } = true; // True = moves with caster, False = static at spawn point

    [ExportGroup("Execution (Travel)")]
    [Export] public bool CloseDistance { get; set; } = false; // Should caster run to target?
    [Export] public float CloseDistanceOffset { get; set; } = 1.5f; // Distance from target center to stop
    [Export] public MultiTargetApproachStrategy ApproachStrategy { get; set; } = MultiTargetApproachStrategy.Average;

    [Export] public string ExecutionAnimation { get; set; } = "Attack"; // Used for melee
    [Export] public PackedScene TravelVfx { get; set; } // Projectile/Ripple
    [Export] public float TravelDelay { get; set; } = 0.0f; // Delay start of travel after Execution phase begins
    [Export] public float TravelDuration { get; set; } = 0.5f; // Time to reach target

    [ExportGroup("Reaction (Impact)")]
    [Export] public PackedScene ImpactVfx { get; set; }
    [Export] public Vector3 ImpactVfxOffset { get; set; } = new Vector3(0, 1.0f, 0);
    [Export] public float ImpactVfxPrewarm { get; set; } = 0.0f; // Spawns VFX this many seconds BEFORE impact.
    [Export] public string TargetHitAnimation { get; set; } = "Hit";
}
