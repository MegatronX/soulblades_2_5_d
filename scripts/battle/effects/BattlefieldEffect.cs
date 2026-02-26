using Godot;

/// <summary>
/// Base class for global battlefield modifiers (weather, terrain, anomalies).
/// These are scene-level effects that can modify action flow for all combatants.
/// </summary>
[GlobalClass]
public abstract partial class BattlefieldEffect : Resource, IActionModifier, IPrioritizedModifier
{
    [Export]
    public string EffectName { get; private set; } = "Battlefield Effect";

    [Export]
    public bool IsActive { get; private set; } = true;

    [Export]
    public int Priority { get; private set; } = 0;

    public virtual void OnActionInitiated(ActionContext context, Node owner) { }
    public virtual void OnActionBroadcast(ActionContext context, Node owner) { }
    public virtual void OnAllyActionInitiated(ActionContext context, Node initiator, Node owner) { }
    public virtual void OnActionTargeted(ActionContext context, Node owner) { }
    public virtual TargetingType ModifyAllowedTargeting(TargetingType currentAllowed) => currentAllowed;
    public virtual void OnActionPostExecution(ActionContext context, Node owner, ActionResult result) { }
}
