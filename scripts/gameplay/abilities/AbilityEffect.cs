using Godot;

/// <summary>
/// Base class for triggered ability effects. Effects declare which triggers they respond to,
/// and AbilityManager dispatches them with a context object at runtime.
/// </summary>
[GlobalClass]
public abstract partial class AbilityEffect : Resource, IPrioritizedModifier
{
    [ExportGroup("Priority")]
    [Export]
    public int Priority { get; private set; } = 0;

    [Export(PropertyHint.Flags, "BattleStart,BattleEnd,TurnStart,TurnEnd,ActionExecuting,ActionExecuted,Targeting,ItemUse,ItemConsume,ExperienceGain,ApGain,EncounterRoll,TimedHitResolved,DamageCalculated,DamageApplied,CostCalculated")]
    public AbilityTrigger TriggerMask { get; private set; } = AbilityTrigger.None;

    [ExportGroup("Triggered VFX")]
    [Export(PropertyHint.ResourceType, "PackedScene")]
    public PackedScene TriggerVfx { get; private set; }

    [Export]
    public Vector3 TriggerVfxOffset { get; private set; } = Vector3.Zero;

    [Export]
    public bool AttachTriggerVfxToOwner { get; private set; } = true;

    [Export]
    public bool RequireExplicitTrigger { get; private set; } = true;

    [ExportGroup("Visual Effects")]
    [Export]
    public Godot.Collections.Array<Resource> VisualEffects { get; private set; } = new();

    public bool Matches(AbilityTrigger trigger)
    {
        return (TriggerMask & trigger) != 0;
    }

    public abstract void Apply(AbilityEffectContext context);
}
