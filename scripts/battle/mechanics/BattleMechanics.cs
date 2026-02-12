using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The "Rules Engine" of the battle system.
/// Responsible for calculating outcomes, applying modifiers, and enforcing game rules.
/// It does NOT handle animations, timing, or flow control.
/// </summary>
[GlobalClass]
public partial class BattleMechanics : Node
{
    [Export] private CalculationStrategy _defaultStrategy;
    
    private IRandomNumberGenerator _rng;

    public override void _Ready()
    {
        _rng = new GodotRandomNumberGenerator();
    }

    public void SetRNG(IRandomNumberGenerator rng)
    {
        _rng = rng;
    }

    /// <summary>
    /// Phase 1: Apply modifiers from the initiator and their allies.
    /// </summary>
    public void ProcessInitiation(ActionContext context, IEnumerable<Node> allies)
    {
        // A. Initiator Modifiers (e.g. "Charge Up")
        foreach (var modifier in GetActionModifiersFrom(context.Initiator))
        {
            modifier.OnActionInitiated(context, context.Initiator);
        }

        // B. Ally Modifiers (e.g. "Commander's Aura")
        foreach (var ally in allies)
        {
            foreach (var modifier in GetActionModifiersFrom(ally))
            {
                modifier.OnAllyActionInitiated(context, context.Initiator, ally);
            }
        }
    }

    /// <summary>
    /// Phase 2: Apply global modifiers from all combatants.
    /// </summary>
    public void ProcessGlobalMods(ActionContext context, IEnumerable<Node> allCombatants)
    {
        // Global Interception (e.g. "Storm Drain")
        foreach (var combatant in allCombatants)
        {
            foreach (var modifier in GetActionModifiersFrom(combatant))
            {
                modifier.OnActionBroadcast(context, combatant);
            }
        }
    }

    /// <summary>
    /// Phase 3: Create per-target contexts and apply incoming modifiers.
    /// </summary>
    public List<ActionContext> ProcessTargeting(ActionContext masterContext, IEnumerable<Node> allCombatants)
    {
        var finalContexts = new List<ActionContext>();

        foreach (var originalTarget in masterContext.InitialTargets)
        {
            // A. Create context for this target
            var targetContext = new ActionContext(masterContext, originalTarget);
            targetContext.Stage = ActionStage.Targeting;

            // B. Target Modifiers (e.g. "Resist", "Reflect")
            foreach (var modifier in GetActionModifiersFrom(originalTarget))
            {
                modifier.OnActionTargeted(targetContext, originalTarget);
            }

            // C. Target Ally Modifiers (e.g. "Cover")
            // We need to find allies of the *current* target (which might have changed in step B)
            // For simplicity here, we iterate all and check alliance, or pass in a lookup.
            // Assuming we can filter allies from allCombatants:
            var targetAllies = GetAlliesOf(originalTarget, allCombatants);
            foreach (var ally in targetAllies)
            {
                foreach (var modifier in GetActionModifiersFrom(ally))
                {
                    modifier.OnActionTargeted(targetContext, ally);
                }
            }

            finalContexts.Add(targetContext);
        }

        return finalContexts;
    }

    /// <summary>
    /// Phase 4: Calculate Hit, Crit, and raw numbers without applying them.
    /// </summary>
    public void CalculatePreliminary(IEnumerable<ActionContext> contexts)
    {
        foreach (var ctx in contexts)
        {
            ctx.Stage = ActionStage.Calculating;
            var strategy = ctx.SourceAction.CalculationStrategy ?? _defaultStrategy;
            if (strategy == null) continue;

            var result = ctx.GetResult(ctx.CurrentTarget);

            // 1. Hit Calculation
            result.IsHit = strategy.CalculateHit(ctx, ctx.CurrentTarget, _rng);

            if (result.IsHit)
            {
                // 2. Crit Calculation (skip for fixed damage to avoid randomness)
                if (!ctx.SourceAction.Flags.HasFlag(ActionFlags.FixedDamage))
                {
                    result.IsCritical = strategy.CalculateCrit(ctx, ctx.CurrentTarget, _rng);
                }
                else
                {
                    result.IsCritical = false;
                }
            }
        }
    }

    /// <summary>
    /// Phase 6: Calculate final damage/healing and apply to stats.
    /// </summary>
    public void CalculateFinalAndApply(IEnumerable<ActionContext> contexts)
    {
        foreach (var ctx in contexts)
        {
            ctx.Stage = ActionStage.Finalized;
            var strategy = ctx.SourceAction.CalculationStrategy ?? _defaultStrategy;
            var result = ctx.GetResult(ctx.CurrentTarget);

            if (ctx.CurrentTarget is Node3D target3D)
            {
                result.HasTargetWorldPosition = true;
                result.TargetWorldPosition = target3D.GlobalPosition;
            }

            if (result.IsHit)
            {
                // 3. Final Damage Calculation (includes Timed Hit flags from animation phase)
                if (strategy != null)
                {
                    result.FinalDamage = strategy.CalculateDamage(ctx, ctx.CurrentTarget, result, _rng);
                }

                // Apply Elemental Resistances
                var damageComponent = ctx.GetComponent<DamageComponent>();
                var elementalComponent = ctx.CurrentTarget.GetNodeOrNull<ElementalComponent>(ElementalComponent.NodeName);

                if (damageComponent != null && elementalComponent != null && damageComponent.ElementalWeights.Count > 0)
                {
                    float totalMultiplier = 0f;
                    float totalWeight = 0f;

                    foreach (var kvp in damageComponent.ElementalWeights)
                    {
                        float weight = kvp.Value;
                        float resistance = elementalComponent.GetResistanceMultiplier(kvp.Key);
                        totalMultiplier += weight * resistance;
                        totalWeight += weight;
                    }

                    // If weights don't sum to 1.0, assume the remainder is neutral (1.0 multiplier)
                    if (totalWeight < 1.0f)
                    {
                        totalMultiplier += (1.0f - totalWeight);
                    }

                    result.FinalDamage = Mathf.RoundToInt(result.FinalDamage * totalMultiplier);
                }

                // 4. Apply to Stats
                var targetStats = ctx.CurrentTarget.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
                if (targetStats != null)
                {
                    targetStats.ModifyCurrentHP(-result.FinalDamage);
                }

                // 4.5 Apply on-hit status effects
                TryApplyStatusEffectsOnHit(ctx, result);

                // 5. Log Runtime Events
                if (result.IsTimedHit) ctx.RuntimeEvents.Add("TimedHitSuccess");
                if (result.IsCritical) ctx.RuntimeEvents.Add("CriticalHit");
            }
        }
    }

    private void TryApplyStatusEffectsOnHit(ActionContext context, ActionResult result)
    {
        if (context?.SourceAction?.StatusEffectsOnHit == null) return;
        if (!result.IsHit) return;

        var entries = context.SourceAction.StatusEffectsOnHit;
        if (entries.Count == 0) return;

        var target = context.CurrentTarget;
        if (!GodotObject.IsInstanceValid(target)) return;

        var statusManager = target.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (statusManager == null) return;

        foreach (var entry in entries)
        {
            if (entry == null || entry.Effect == null) continue;

            float chance = Mathf.Clamp(entry.ChancePercent, 0f, 100f);
            if (chance <= 0f) continue;

            statusManager.TryApplyEffect(entry.Effect, null, chance, _rng);
        }
    }

    /// <summary>
    /// Enforces costs (MP, HP, Items) on the user.
    /// </summary>
    public void ApplyCosts(ActionContext context)
    {
        // Example: Check for a CostComponent in the action
        // var costComp = context.GetComponent<CostComponent>();
        // if (costComp != null) { ... deduct MP ... }

        if (context?.SourceItem == null) return;

        var inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
        if (inventory == null) return;

        inventory.TryConsumeItem(context.SourceItem, 1, null, _rng);
    }

    /// <summary>
    /// Phase 7.5: Allow targets to register reactions based on the final outcome.
    /// </summary>
    public void ProcessPostExecution(IEnumerable<ActionContext> contexts)
    {
        foreach (var ctx in contexts)
        {
            var target = ctx.CurrentTarget;
            
            // Check if target is valid (it might have been freed if it died and didn't persist)
            if (!GodotObject.IsInstanceValid(target)) continue;
            
            // If the target was killed by this action, they cannot react (e.g. Counter Attack).
            var stats = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
            if (stats != null && stats.CurrentHP <= 0) continue;

            var result = ctx.GetResult(target);
            
            foreach (var modifier in GetActionModifiersFrom(target))
            {
                modifier.OnActionPostExecution(ctx, target, result);
            }
        }
    }

    // --- Helpers ---

    private IEnumerable<IActionModifier> GetActionModifiersFrom(Node character)
    {
        return character.FindChildren("*", recursive: true).OfType<IActionModifier>();
    }

    private IEnumerable<Node> GetAlliesOf(Node character, IEnumerable<Node> allCombatants)
    {
        // Simple group-based check. 
        // If character is Player, allies are Players. If Enemy, allies are Enemies.
        bool isPlayer = character.IsInGroup(GameGroups.PlayerCharacters);
        return allCombatants.Where(c => c != character && c.IsInGroup(GameGroups.PlayerCharacters) == isPlayer);
    }
}
