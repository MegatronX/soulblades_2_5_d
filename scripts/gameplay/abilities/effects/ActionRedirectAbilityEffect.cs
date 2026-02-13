using Godot;

/// <summary>
/// Ability effect that redirects actions based on criteria (e.g., Storm Drain).
/// Implemented as an action modifier so it can intercept incoming actions.
/// </summary>
[GlobalClass]
public partial class ActionRedirectAbilityEffect : AbilityEffect, IActionModifier
{
    [ExportGroup("Redirect Rules")]
    [Export]
    public ActionRedirectDefinition RedirectDefinition { get; set; }

    public override void Apply(AbilityEffectContext context)
    {
        // This effect operates through IActionModifier hooks.
    }

    public void OnActionBroadcast(ActionContext context, Node owner)
    {
        if (RedirectDefinition == null || RedirectDefinition.Phase != ActionRedirectPhase.Broadcast) return;
        if (RedirectDefinition.TryRedirect(context, owner, isBroadcast: true))
        {
            PlayTriggerVfx(owner);
        }
    }

    public void OnActionTargeted(ActionContext context, Node owner)
    {
        if (RedirectDefinition == null || RedirectDefinition.Phase != ActionRedirectPhase.Targeted) return;
        if (RedirectDefinition.TryRedirect(context, owner, isBroadcast: false))
        {
            PlayTriggerVfx(owner);
        }
    }

    private void PlayTriggerVfx(Node owner)
    {
        if (TriggerVfx == null || owner == null) return;
        if (!GodotObject.IsInstanceValid(owner)) return;

        var eventBus = owner.GetNodeOrNull<GlobalEventBus>(GlobalEventBus.Path);
        if (eventBus == null) return;

        eventBus.EmitSignal(GlobalEventBus.SignalName.EffectVfxRequested, TriggerVfx, owner, TriggerVfxOffset, AttachTriggerVfxToOwner);
    }
}
