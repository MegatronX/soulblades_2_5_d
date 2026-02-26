using Godot;

/// <summary>
/// Reusable visual effect resource that can be attached to statuses and abilities.
/// </summary>
[GlobalClass]
public abstract partial class BattleVisualEffect : Resource, IPrioritizedModifier
{
    [ExportGroup("Priority")]
    [Export]
    public int Priority { get; private set; } = 0;

    [Export(PropertyHint.Flags, "Persistent,StatusApplied,StatusRemoved,TurnStart,TurnEnd,ActionInitiated,AllyActionInitiated,ActionBroadcast,ActionTargeted,ActionPostExecution,AbilityTriggered")]
    public BattleVisualEventType TriggerMask { get; private set; } = BattleVisualEventType.Persistent;

    public bool Matches(BattleVisualEventType eventType)
    {
        return (TriggerMask & eventType) != 0;
    }

    public virtual void OnEvent(BattleVisualEffectContext context)
    {
    }

    public virtual void ContributePersistent(BattleVisualStateAccumulator state, BattleVisualEffectContext context)
    {
    }
}
