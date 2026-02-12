using Godot;
using System.Linq;

[GlobalClass]
public partial class ItemActionRequirements : Resource
{
    [Export] public bool RequireAll { get; set; } = true;

    [ExportGroup("Abilities")]
    [Export] public bool RequireEquippedAbilities { get; set; } = false;
    [Export] public Godot.Collections.Array<Ability> RequiredAbilities { get; set; } = new();

    [ExportGroup("Actions")]
    [Export] public Godot.Collections.Array<ActionData> RequiredActions { get; set; } = new();

    public bool IsSatisfied(Node user)
    {
        if (user == null) return false;

        bool hasAbilityRequirement = RequiredAbilities != null && RequiredAbilities.Count > 0;
        bool hasActionRequirement = RequiredActions != null && RequiredActions.Count > 0;
        if (!hasAbilityRequirement && !hasActionRequirement) return true;

        bool abilityOk = !hasAbilityRequirement || HasRequiredAbilities(user);
        bool actionOk = !hasActionRequirement || HasRequiredActions(user);

        if (RequireAll)
        {
            return abilityOk && actionOk;
        }

        return abilityOk || actionOk;
    }

    private bool HasRequiredAbilities(Node user)
    {
        var manager = user.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
        if (manager == null) return false;

        var abilities = RequireEquippedAbilities ? manager.GetEquippedAbilities() : manager.GetKnownAbilities();
        if (abilities == null) return false;

        foreach (var required in RequiredAbilities)
        {
            if (required == null) continue;
            if (!abilities.Contains(required)) return false;
        }

        return true;
    }

    private bool HasRequiredActions(Node user)
    {
        var manager = user.GetNodeOrNull<ActionManager>(ActionManager.DefaultName);
        if (manager == null) return false;

        foreach (var required in RequiredActions)
        {
            if (required == null) continue;
            if (!manager.LearnedActions.OfType<ActionData>().Contains(required))
            {
                return false;
            }
        }

        return true;
    }
}
