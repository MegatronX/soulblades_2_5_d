using Godot;

/// <summary>
/// Shared redirect logic used by both status effects and abilities.
/// </summary>
[GlobalClass]
public partial class ActionRedirectDefinition : Resource
{
    [ExportGroup("Redirect Rules")]
    [Export]
    public ActionRedirectCriteria Criteria { get; set; }

    [Export]
    public ActionRedirectPhase Phase { get; set; } = ActionRedirectPhase.Broadcast;

    [Export]
    public ActionRedirectTarget Target { get; set; } = ActionRedirectTarget.Owner;

    [Export]
    public string Message { get; set; } = string.Empty;

    public bool TryRedirect(ActionContext context, Node owner, bool isBroadcast)
    {
        if (context == null || owner == null) return false;
        if (Criteria == null) return false;
        if (!IsValidRedirectTarget(owner)) return false;
        if (!Criteria.Matches(context, owner)) return false;

        var redirectTarget = ResolveRedirectTarget(context, owner);
        if (!GodotObject.IsInstanceValid(redirectTarget)) return false;

        if (isBroadcast)
        {
            if (context.InitialTargets.Count == 1 && context.InitialTargets[0] == redirectTarget)
            {
                return false;
            }

            context.InitialTargets.Clear();
            context.InitialTargets.Add(redirectTarget);
        }
        else
        {
            if (context.CurrentTarget == redirectTarget)
            {
                return false;
            }
        }

        context.CurrentTarget = redirectTarget;
        context.WasRedirected = true;

        if (!string.IsNullOrWhiteSpace(Message))
        {
            context.ModificationLog.Add(Message);
        }

        return true;
    }

    private Node ResolveRedirectTarget(ActionContext context, Node owner)
    {
        return Target switch
        {
            ActionRedirectTarget.Initiator => context.Initiator,
            _ => owner
        };
    }

    private static bool IsValidRedirectTarget(Node owner)
    {
        if (!GodotObject.IsInstanceValid(owner)) return false;
        var stats = owner.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        return stats == null || stats.CurrentHP > 0;
    }
}
