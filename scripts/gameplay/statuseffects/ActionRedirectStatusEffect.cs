using Godot;

public enum ActionRedirectPhase
{
    Broadcast,
    Targeted
}

public enum ActionRedirectTarget
{
    Owner,
    Initiator
}

/// <summary>
/// Generic redirector for actions. Uses a criteria resource to decide eligibility.
/// </summary>
[GlobalClass]
public partial class ActionRedirectStatusEffect : StatusEffect
{
    [ExportGroup("Redirect Rules")]
    [Export]
    public ActionRedirectDefinition RedirectDefinition { get; set; }

    public override void OnActionBroadcast(ActionContext context, Node owner)
    {
        if (RedirectDefinition == null || RedirectDefinition.Phase != ActionRedirectPhase.Broadcast) return;
        if (RedirectDefinition.TryRedirect(context, owner, isBroadcast: true))
        {
            RequestTriggerAnimation(owner);
        }
    }

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        if (RedirectDefinition == null || RedirectDefinition.Phase != ActionRedirectPhase.Targeted) return;
        if (RedirectDefinition.TryRedirect(context, owner, isBroadcast: false))
        {
            RequestTriggerAnimation(owner);
        }
    }
}
