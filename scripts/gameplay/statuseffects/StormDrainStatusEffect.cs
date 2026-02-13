using Godot;

[GlobalClass]
public partial class StormDrainStatusEffect : ActionRedirectStatusEffect
{
    public StormDrainStatusEffect()
    {
        if (RedirectDefinition != null) return;

        RedirectDefinition = new ActionRedirectDefinition
        {
            Phase = ActionRedirectPhase.Broadcast,
            Target = ActionRedirectTarget.Owner,
            Message = "Drawn in by Storm Drain!",
            Criteria = new ActionRedirectCriteria
            {
                RequireElementMajority = true,
                RequiredElement = ElementType.Water,
                MinElementRatio = 0.5f,
                RequireNotRedirected = true
            }
        };
    }
}
