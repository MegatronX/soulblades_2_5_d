using Godot;

/// <summary>
/// A Resource that defines the blueprint for a battle action.
/// It is composed of modular ActionComponentData resources.
/// </summary>
[GlobalClass]
public partial class ActionData : BattleCommand
{
    [Export]
    public string Category { get; private set; } = "General";

    [Export]
    public float TickCost { get; private set; } = 0f; // 0 = Standard Turn (1000 ticks). Positive values add delay.

    [Export(PropertyHint.Range, "0.0,1.0,0.05")]
    public float PhysicalRatio { get; private set; } = 1.0f; // 1.0 = 100% Physical, 0.0 = 100% Magical

    [Export(PropertyHint.Range, "0,100,1")]
    public int CritChance { get; private set; } = 5; // Base critical hit chance percentage.

    [Export]
    public ActionFlags Flags { get; private set; }

    [Export]
    public ActionIntent Intent { get; private set; } = ActionIntent.Aggressive;

    [Export]
    public TargetingType BaseTargeting { get; private set; } = TargetingType.AnySingleTarget;

    [Export]
    public TargetingType AllowedTargeting { get; private set; } = TargetingType.AnyEnemy | TargetingType.AnyEnemyParty | TargetingType.All;

    [Export]
    public bool CanTargetDead { get; private set; } = false;

    [ExportGroup("Mechanics")]
    [Export]
    public CalculationStrategy CalculationStrategy { get; private set; } = null;

    [ExportGroup("Timed Hits")]
    [Export]
    public Godot.Collections.Array<TimedHitSettings> TimedHitSettings { get; private set; } = new();

    [ExportGroup("Visuals")]
    [Export]
    public VisualActionSettings VisualSettings { get; private set; }

    [ExportGroup("Components")]
    /// <summary>
    /// The modular building blocks of this action.
    /// Drag and drop ActionComponentData resources here in the editor.
    /// </summary>
    [Export]
    public Godot.Collections.Array<ActionComponentData> Components { get; private set; } = new();
}