using Godot;
using System.Linq;

/// <summary>
/// Rules used by a redirecting status effect or ability to decide if an incoming
/// action should be redirected.
/// </summary>
[GlobalClass]
public partial class ActionRedirectCriteria : Resource
{
    [ExportGroup("General")]
    [Export]
    public bool RequireNotRedirected { get; set; } = true;

    [Export]
    public bool RequireOwnerIsCurrentTarget { get; set; } = false;

    [Export]
    public bool RequireSingleTarget { get; set; } = false;

    [Export]
    public bool RequireSourceItem { get; set; } = false;

    [Export]
    public bool RequireInitiatorIsNotOwner { get; set; } = false;

    [Export]
    public bool RespectNonRedirectableFlag { get; set; } = true;

    [ExportGroup("Action Category")]
    [Export]
    public bool RequireCategory { get; set; } = false;

    [Export]
    public ActionCategory RequiredCategory { get; set; } = ActionCategory.General;

    [ExportGroup("Magic Ratio")]
    [Export]
    public bool RequireMagicRatio { get; set; } = false;

    [Export(PropertyHint.Range, "0.0,1.0,0.05")]
    public float MinMagicRatio { get; set; } = 0.5f;

    [ExportGroup("Elemental Ratio")]
    [Export]
    public bool RequireElementMajority { get; set; } = false;

    [Export]
    public ElementType RequiredElement { get; set; } = ElementType.None;

    [Export(PropertyHint.Range, "0.0,1.0,0.05")]
    public float MinElementRatio { get; set; } = 0.5f;

    public bool Matches(ActionContext context, Node owner)
    {
        if (context == null) return false;

        if (RespectNonRedirectableFlag && context.SourceAction != null &&
            context.SourceAction.Flags.HasFlag(ActionFlags.CannotBeRedirected))
        {
            return false;
        }

        if (RequireNotRedirected && context.WasRedirected) return false;
        if (RequireOwnerIsCurrentTarget && context.CurrentTarget != owner) return false;
        if (RequireSingleTarget && context.InitialTargets != null && context.InitialTargets.Count > 1) return false;
        if (RequireSourceItem && context.SourceItem == null) return false;
        if (RequireInitiatorIsNotOwner && context.Initiator == owner) return false;

        if (RequireCategory)
        {
            if (context.SourceAction == null || context.SourceAction.Category != RequiredCategory)
            {
                return false;
            }
        }

        if (RequireMagicRatio)
        {
            if (context.SourceAction == null) return false;
            float magicRatio = 1.0f - context.SourceAction.PhysicalRatio;
            if (magicRatio < MinMagicRatio) return false;
        }

        if (RequireElementMajority)
        {
            if (!IsElementRatioAtLeast(context, RequiredElement, MinElementRatio))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsElementRatioAtLeast(ActionContext context, ElementType element, float minRatio)
    {
        if (context == null || element == ElementType.None) return false;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null || damage.ElementalWeights == null || damage.ElementalWeights.Count == 0) return false;

        float total = damage.ElementalWeights.Values.Where(v => v > 0f).Sum();
        if (total <= 0f) return false;

        damage.ElementalWeights.TryGetValue(element, out float elementWeight);
        if (elementWeight <= 0f) return false;

        return (elementWeight / total) >= minRatio;
    }
}
