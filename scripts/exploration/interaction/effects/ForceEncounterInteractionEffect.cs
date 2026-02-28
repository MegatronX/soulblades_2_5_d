using Godot;

/// <summary>
/// Triggers a random/scripted encounter from the active map encounter manager.
/// </summary>
[GlobalClass]
public partial class ForceEncounterInteractionEffect : InteractionEffect
{
    public override void Execute(ExplorationInteractionContext context, Node source)
    {
        var encounterManager = context?.MapController?.EncounterManager;
        if (encounterManager == null)
        {
            GD.PrintErr("[ForceEncounterInteractionEffect] EncounterManager not found on map.");
            return;
        }

        encounterManager.ForceEncounter();
    }
}
