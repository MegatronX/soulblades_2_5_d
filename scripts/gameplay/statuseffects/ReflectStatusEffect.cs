using Godot;

[GlobalClass]
public partial class ReflectStatusEffect : ActionRedirectStatusEffect
{
    public ReflectStatusEffect()
    {
        if (RedirectDefinition != null) return;

        RedirectDefinition = new ActionRedirectDefinition
        {
            Phase = ActionRedirectPhase.Targeted,
            Target = ActionRedirectTarget.Initiator,
            Message = "Reflected!",
            Criteria = new ActionRedirectCriteria
            {
                RequireMagicRatio = true,
                MinMagicRatio = 0.5f,
                RequireOwnerIsCurrentTarget = true,
                RequireNotRedirected = true,
                RequireInitiatorIsNotOwner = true
            }
        };
    }
}
