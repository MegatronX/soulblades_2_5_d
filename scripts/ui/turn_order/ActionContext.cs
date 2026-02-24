using Godot;
using System.Collections.Generic;
using System.Linq;

public enum ActionStage
{
    Initiated,
    Targeting,
    Calculating,
    Animating,
    Finalized
}

/// <summary>
/// A temporary, "in-flight" object representing an action being executed.
/// This object is created from ActionData and can be freely modified by other
/// game systems (like status effects) before and during execution.
/// </summary>
[GlobalClass]
public partial class ActionContext : RefCounted
{
    public ActionStage Stage { get; set; } = ActionStage.Initiated;

    public ActionData SourceAction { get; }
    public ItemData SourceItem { get; }
    public Node Initiator { get; }
    public List<Node> InitialTargets { get; }

    // The target for this specific instance of the context. Can be changed by modifiers.
    public Node CurrentTarget { get; set; }

    // The live, mutable list of runtime components.
    public List<ActionComponent> Components { get; } = new();

    // A log of modifications for animation/UI purposes.
    public List<string> ModificationLog { get; } = new();

    // A set to store runtime event tags like "TimedHitSuccess" or "CriticalHit".
    // This is checked by conditions for chain actions.
    public HashSet<string> RuntimeEvents { get; } = new();

    // Map of Target -> Calculated Result
    public Dictionary<Node, ActionResult> Results { get; } = new();

    // Actions triggered in response to this action (e.g. Counter Attacks).
    public List<ActionContext> PendingReactions { get; } = new();

    // Cumulative damage multiplier from timed hits (starts at 1.0 = 100%).
    public float TimedHitMultiplier { get; set; } = 1.0f;

    // Store the rating of the last timed hit for UI purposes
    public TimedHitRating LastTimedHitRating { get; set; } = TimedHitRating.Miss;

    // Additional critical chance from runtime effects (e.g. Bleed vulnerability).
    public float BonusCritChancePercent { get; set; } = 0.0f;

    // Scalar applied to final calculated damage/healing.
    public float ActionPowerScalar { get; set; } = 1.0f;

    // Runtime adjustment to the selected action's TickCost.
    public float TickCostAdjustment { get; set; } = 0.0f;

    // Used by follow-up effects (e.g. Echo Cast) to skip duplicate cost application.
    public bool SkipActionCosts { get; set; } = false;

    // Runtime status payloads applied on hit (e.g. Hexed Edge).
    public List<StatusEffectChanceEntry> ExtraStatusEffectsOnHit { get; } = new();

    /// <summary>
    /// Set to true when an action's target has been redirected to avoid infinite ping-pong.
    /// </summary>
    public bool WasRedirected { get; set; } = false;

    /// <summary>
    /// Master constructor to create the initial context from an ActionData resource.
    /// </summary>
    public ActionContext(ActionData source, Node initiator, IEnumerable<Node> targets, ItemData sourceItem = null)
    {
        SourceAction = source;
        SourceItem = sourceItem;
        Initiator = initiator;
        InitialTargets = new List<Node>(targets);

        // If this is an AttackData, synthesize a DamageComponent automatically.
        if (source is AttackData attackData)
        {
            var damageComp = new DamageComponent(new DamageComponentData 
            { 
                // We create a temporary data wrapper or just set values directly if DamageComponent supports it.
                // Since DamageComponent takes Data in constructor, we create a transient data object.
                // Note: In a real scenario, you might want DamageComponent to have a parameterless constructor or one taking raw values.
                // For now, we assume we can modify the component after creation or pass data.
            });
            damageComp.Power = attackData.Power;
            damageComp.Accuracy = attackData.Accuracy;
            if (attackData.Element != ElementType.None)
            {
                damageComp.ElementalWeights[attackData.Element] = 1.0f;
            }
            Components.Add(damageComp);
        }

        // Create live, mutable component instances from the data resources.
        foreach (var componentData in source.Components)
        {
            Components.Add(ActionComponent.CreateRuntimeComponent(componentData));
        }
    }

    /// <summary>
    /// Copy constructor for creating a unique, modifiable context for a single target.
    /// </summary>
    public ActionContext(ActionContext original, Node currentTarget)
    {
        SourceAction = original.SourceAction;
        SourceItem = original.SourceItem;
        Initiator = original.Initiator;
        InitialTargets = original.InitialTargets;
        CurrentTarget = currentTarget;
        WasRedirected = original.WasRedirected;
        BonusCritChancePercent = original.BonusCritChancePercent;
        ActionPowerScalar = original.ActionPowerScalar;
        TickCostAdjustment = original.TickCostAdjustment;
        SkipActionCosts = original.SkipActionCosts;
        ModificationLog = new List<string>(); // Each target gets a fresh log.
        ExtraStatusEffectsOnHit = new List<StatusEffectChanceEntry>(original.ExtraStatusEffectsOnHit);

        // Create deep copies of the runtime components so modifications are isolated per target.
        foreach (var component in original.Components)
        {
            Components.Add(component.DeepCopy());
        }
    }

    public T GetComponent<T>() where T : ActionComponent => Components.OfType<T>().FirstOrDefault();

    public ActionResult GetResult(Node target)
    {
        if (!Results.ContainsKey(target))
        {
            Results[target] = new ActionResult();
        }
        return Results[target];
    }
}
