using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the turn order for a battle based on character speed.
/// This system calculates the "time to next turn" for each combatant,
/// making it efficient to generate long turn-order previews.
/// </summary>
public partial class TurnManager : Node
{
    public class SimulatedStatusEffect
    {
        public StatusEffect Data;
        public int RemainingTurns;
    }

    // A data structure to hold the turn state for a single combatant.
    public partial class TurnData : RefCounted
    {
        // These are for the "real" state
        public Node Combatant { get; set; }
        public StatsComponent Stats { get; }
        public StatusEffectManager StatusEffects { get; }
        public float Counter { get; set; } // The current "tick" value for this combatant.
        public float TickValue { get; set; } // The tick value at which this turn occurs.

        // This is for the "simulated" state
        public SimulatedStats SimStats { get; private set; }
        public List<SimulatedStatusEffect> SimEffects { get; private set; }

        // A parameter-less constructor is required for Godot to be able to instantiate it from C++.
        public TurnData()
        {
        }

        public TurnData(Node combatant) // This constructor is for creating new instances
        {
            Combatant = combatant;
            Stats = combatant.GetNode<StatsComponent>(StatsComponent.NodeName);
            StatusEffects = combatant.GetNodeOrNull<StatusEffectManager>("StatusEffectManager");
            TickValue = -1; // Not yet calculated
            Counter = 0;
            SimStats = new SimulatedStats(Stats); // Always create a base simulation
        }

        // A copy constructor for creating simulation instances.
        public TurnData(TurnData original)
        {
            Combatant = original.Combatant;
            Counter = original.Counter;
            TickValue = original.TickValue;

            // ALWAYS create a new SimulatedStats object for a new simulation context.
            SimStats = original.SimStats != null ? new SimulatedStats(original.SimStats) : new SimulatedStats(original.Stats);
            Stats = original.Stats; // Keep a reference to the real stats for comparison if needed.
            StatusEffects = original.StatusEffects;

            // Initialize Simulated Effects
            if (original.SimEffects != null)
            {
                // Deep copy existing simulation
                SimEffects = original.SimEffects.Select(e => new SimulatedStatusEffect 
                { 
                    Data = e.Data, 
                    RemainingTurns = e.RemainingTurns 
                }).ToList();
            }
            else if (original.StatusEffects != null)
            {
                // Create simulation from real state
                SimEffects = original.StatusEffects.GetActiveEffects()
                    .Select(e => new SimulatedStatusEffect 
                    { 
                        Data = e.EffectData, 
                        RemainingTurns = e.RemainingTurns 
                    }).ToList();
            }
            else
            {
                SimEffects = new List<SimulatedStatusEffect>();
            }
        }

        // If we have a simulation, check that. Otherwise check real state.
        public bool IsBlocked => SimEffects != null ? SimEffects.Any(e => e.Data.IsTurnSkipping) 
                                                    : (StatusEffects?.HasTurnSkippingEffect() ?? false);
    }

    /// <summary>
    /// A data structure to define a hypothetical action for turn order previews.
    /// Must inherit from RefCounted to be passed through signals.
    /// </summary>
    public partial class ActionPreview : RefCounted
    {
        public float ActionTickCost { get; set; }
        // A map of Target -> (StatType -> Modifier)
        public Dictionary<Node, Dictionary<StatType, Tuple<int, float>>> StatusEffectSim { get; set; } = new();
        // A map of Target -> Counter Change
        public Dictionary<Node, float> CounterChangeSim { get; set; } = new();
        // A map of Target -> List of Effects to Apply
        public Dictionary<Node, List<StatusEffect>> AppliedEffects { get; set; } = new();

        // A parameterless constructor is required for Godot to be able to instantiate it from C++.
        public ActionPreview()
        {
        }
    }

    /// <summary>
    /// The number of ticks required to get a turn.
    /// </summary>
    public const float TickThreshold = 1000f;

    private readonly List<TurnData> _combatants = new();

    /// <summary>
    /// Adds a combatant to the turn order, optionally with a randomized starting counter.
    /// </summary>
    /// <param name="combatant">The character node to add.</param>
    /// <param name="initialCounter">The starting value for the turn counter.</param>
    public void AddCombatant(Node combatant, float initialCounter = 0f)
    {
        var turnData = new TurnData(combatant) { Counter = initialCounter };
        _combatants.Add(turnData);
    }

    public void RemoveCombatant(TurnData combatant)
    {
        _combatants.RemoveAll(c => c == combatant);
    }

    public IReadOnlyCollection<TurnData> GetCombatants()
    {
        return _combatants;
    }


    /// <summary>
    /// Calculates and returns the next combatant to take a turn without modifying the game state.
    /// </summary>
    public TurnData GetNextTurn()
    {
        return GenerateTurnOrder(1).FirstOrDefault();
    }

    /// <summary>
    /// This is the core of the preview system. It generates a list of upcoming turns
    /// based on the current state, without altering it.
    /// </summary>
    /// <param name="turnCount">How many turns to predict into the future.</param>
    /// <param name="includeBlocked">If false, blocked combatants are simulated but not added to the returned list.</param>
    /// <returns>A list of TurnData objects representing the turn order.</returns>
    public List<TurnData> GenerateTurnOrder(int turnCount, bool includeBlocked = true)
    {
        // This is a convenience overload that uses the current game state.
        return GenerateTurnOrder(turnCount, _combatants, 0f, includeBlocked);
    }

    /// <summary>
    /// Generates a turn order preview based on a hypothetical action being taken.
    /// </summary>
    /// <param name="turnCount">How many turns to predict.</param>
    /// <param name="actor">The combatant who is hypothetically taking the action.</param>
    /// <param name="preview">The details of the hypothetical action.</param>
    /// <returns>A list of TurnData objects representing the predicted turn order.</returns>
    public List<TurnData> PreviewAction(int turnCount, TurnData actor, ActionPreview preview)
    {
        if (turnCount < 1) return new List<TurnData>();

        // --- Step 1: Create a deep copy of the current combatant states for simulation ---
        var simulation = _combatants.Select(c => new TurnData(c)).ToList();
        var previewTurnOrder = new List<TurnData>();

        // --- Step 2: Add the current actor to the top of the list ---
        // We must create a snapshot that matches what GenerateTurnOrder produces for the current turn.
        // GenerateTurnOrder subtracts TickThreshold from the counter for the snapshot.
        var currentTurnSnapshot = new TurnData(actor);
        currentTurnSnapshot.Counter -= TickThreshold;
        currentTurnSnapshot.TickValue = currentTurnSnapshot.Counter;
        previewTurnOrder.Add(currentTurnSnapshot);

        // --- Step 3: Find the simulated actor and apply the action's cost and effects. ---
        var simActor = simulation.FirstOrDefault(c => c.Combatant == actor.Combatant);
        if (simActor == null) return new List<TurnData>(); // Should not happen

        // Apply status effect simulations to all targets.
        foreach (var targetSim in preview.StatusEffectSim)
        {
            var simTarget = simulation.FirstOrDefault(c => c.Combatant == targetSim.Key);
            if (simTarget == null) continue;
            // This is where the actor could apply a slow to themselves, affecting subsequent turn calculations.
            foreach (var modifier in targetSim.Value)
            {
                simTarget.SimStats.ApplyModifier(modifier.Key, modifier.Value.Item1, modifier.Value.Item2);
            }
        }

        // Apply hypothetical status effects (e.g. Stop)
        foreach (var kvp in preview.AppliedEffects)
        {
            var simTarget = simulation.FirstOrDefault(c => c.Combatant == kvp.Key);
            if (simTarget == null) continue;

            foreach (var effect in kvp.Value)
            {
                simTarget.SimEffects.Add(new SimulatedStatusEffect
                {
                    Data = effect,
                    RemainingTurns = effect.MinDurationTurns // Use min duration for deterministic preview
                });
            }
        }

        // Manually process the actor's turn within the simulation.
        // This advances all counters and applies the action cost to the actor.
        CommitTurn(simActor, preview.ActionTickCost, simulation);

        // Tick down simulated status effects for the actor (simulating OnTurnEnd)
        if (simActor.SimEffects != null)
        {
            for (int k = simActor.SimEffects.Count - 1; k >= 0; k--)
            {
                simActor.SimEffects[k].RemainingTurns--;
                if (simActor.SimEffects[k].RemainingTurns <= 0)
                {
                    if (simActor.SimEffects[k].Data is StatModifierEffect statMod)
                    {
                        foreach (var entry in statMod.StatMultipliers)
                        {
                            if (entry == null) continue;
                            if (entry.Multiplier == 0) continue;
                            float effective = entry.Multiplier <= 0f ? 1.0f : entry.Multiplier;
                            simActor.SimStats.ApplyModifier(entry.Stat, 0, 1.0f / effective);
                        }
                    }
                    simActor.SimEffects.RemoveAt(k);
                }
            }
        }

        // --- Step 4: Generate the *rest* of the turn order from this new simulation state. ---
        // We ask for one less turn because we've already added the actor.
        var subsequentTurns = GenerateTurnOrder(turnCount - 1, simulation);
        previewTurnOrder.AddRange(subsequentTurns);

        return previewTurnOrder;
    }

    private List<TurnData> GenerateTurnOrder(int turnCount, List<TurnData> sourceCombatants, float initialActionCost = 0f, bool includeBlocked = true)
    {
        // Create a deep copy of the current combatant states to simulate with.
        var simulation = sourceCombatants.Select(c => new TurnData(c)).ToList();
        var turnOrder = new List<TurnData>();

        int turnsGenerated = 0;
        while (turnsGenerated < turnCount)
        {
            TurnData next = null;
            float minTicksToTurn = float.MaxValue;

            foreach (var combatant in simulation)
            {
                // Use simulated stats if they exist, otherwise fall back to real stats.
                float speed = combatant.SimStats?.GetStatValue(StatType.Speed) ?? combatant.Stats.GetStatValue(StatType.Speed);
                if (speed <= 0) continue;

                float currentCounter = combatant.Counter;

                // If this is the first turn of the simulation AND there's an initial action cost,
                // we need to factor that in for the acting character.
                // The actor is the one with the lowest ticksToTurn in the *source* list, which is the first element
                // of a non-preview turn order generation.
                var actor = GetNextTurnFromSource(sourceCombatants);
                if (turnsGenerated == 0 && initialActionCost != 0f && combatant.Combatant == actor?.Combatant)
                {
                    currentCounter -= initialActionCost;
                }

                float ticksToTurn = (TickThreshold - currentCounter) / speed;

                if (ticksToTurn < minTicksToTurn)
                {
                    minTicksToTurn = ticksToTurn;
                    next = combatant;
                }
            }

            if (next == null) break; // No valid combatants left.

            // Advance all combatants' counters by the time it took for the next turn to occur.
            foreach (var combatant in simulation)
            {
                float speed = combatant.SimStats?.GetStatValue(StatType.Speed) ?? combatant.Stats.GetStatValue(StatType.Speed);
                combatant.Counter += speed * minTicksToTurn;
            }

            // The character whose turn it is has their counter "spend" the threshold.
            next.Counter -= TickThreshold;

            // Capture the state *before* ticking down effects, as this represents the turn that just occurred.
            bool wasBlocked = next.IsBlocked;

            // If the combatant is blocked, we simulated the time passage (above),
            // but we might not want to show them in the UI list.
            if (includeBlocked || !wasBlocked)
            {
                // Create a SNAPSHOT of the simulation state at this moment.
                // We cannot return 'next' directly because it will be mutated in future loops.
                var snapshot = new TurnData(next);
                snapshot.TickValue = next.Counter; // Use post-spend counter as ID
                
                turnOrder.Add(snapshot);
                turnsGenerated++;
            }

            // Tick down simulated status effects for the actor
            // This simulates the "OnTurnEnd" phase where durations decrease.
            for (int k = next.SimEffects.Count - 1; k >= 0; k--)
            {
                next.SimEffects[k].RemainingTurns--;
                if (next.SimEffects[k].RemainingTurns <= 0)
                {
                    // If the expired effect was a StatModifierEffect, revert its changes in the simulation.
                    // This ensures that temporary speed boosts (Haste) or slows actually expire in the preview.
                    if (next.SimEffects[k].Data is StatModifierEffect statMod)
                    {
                        foreach (var entry in statMod.StatMultipliers)
                        {
                            if (entry == null) continue;
                            if (entry.Multiplier == 0) continue;
                            float effective = entry.Multiplier <= 0f ? 1.0f : entry.Multiplier;
                            // Revert the multiplication: New = Old * (1 / Multiplier)
                            next.SimStats.ApplyModifier(entry.Stat, 0, 1.0f / effective);
                        }
                    }

                    next.SimEffects.RemoveAt(k);
                }
            }
            
            // Safety break to prevent infinite loops if everyone is blocked forever
            if (turnsGenerated > turnCount * 2 && turnOrder.Count == 0) break;
        }

        // --- DEBUG PRINT ---

        return turnOrder;
    }

    /// <summary>
    /// Helper method to find the next combatant from a given source list without simulation.
    /// This is used to correctly identify the actor for initial action cost previews.
    /// </summary>
    private TurnData GetNextTurnFromSource(List<TurnData> sourceCombatants)
    {
        TurnData next = null;
        float minTicksToTurn = float.MaxValue;

        foreach (var combatant in sourceCombatants)
        {
            // Use real stats for this calculation
            float speed = combatant.Stats.GetStatValue(StatType.Speed);
            if (speed <= 0) continue;

            float ticksToTurn = (TickThreshold - combatant.Counter) / speed;

            if (ticksToTurn < minTicksToTurn)
            {
                minTicksToTurn = ticksToTurn;
                next = combatant;
            }
        }
        return next;
    }


    /// <summary>
    //  This method commits a turn, updating the actual game state.
    /// </summary>
    /// <param name="actingCombatant">The combatant who is taking their turn.</param>
    /// <param name="actionTickCost">The tick cost of the action they performed.</param>    
    /// <param name="actionDirector">The context for the current battle, needed for status effect logic.</param>
    public void CommitTurn(TurnData actingCombatant, float actionTickCost, ActionDirector actionDirector)
    {
        // --- End of Turn ---
        // The turn for the acting combatant is now officially over.
        // Trigger all OnTurnEnd effects for them.
        var actingStatusManager = actingCombatant.Combatant.GetNode<StatusEffectManager>("StatusEffectManager");
        actingStatusManager?.OnTurnEnd(actionDirector);

        // This is a public-facing wrapper that commits the turn to the "real" game state.
        CommitTurn(actingCombatant, actionTickCost, _combatants);

        // --- Start of Next Turn ---
        // Now that the state is updated, find out who is next.
        var nextTurn = GetNextTurn();
        if (nextTurn == null) return;

        // Trigger all OnTurnStart effects for the incoming character.
        var nextStatusManager = nextTurn.Combatant.GetNode<StatusEffectManager>("StatusEffectManager");
        nextStatusManager?.OnTurnStart(actionDirector);
    }

    /// <summary>
    /// Internal turn commit logic that can operate on either the real or a simulated list of combatants.
    /// </summary>
    private void CommitTurn(TurnData actingCombatant, float actionTickCost, List<TurnData> combatantList)
    {
        // Find the actual TurnData object for the combatant who acted.
        var combatantData = combatantList.FirstOrDefault(c => c.Combatant == actingCombatant.Combatant);
        if (combatantData == null)
        {
            GD.PrintErr($"Could not find combatant {actingCombatant.Combatant.Name} in TurnManager to commit turn.");
            return;
        }

        // Use simulated stats if available for the calculation, as this might be a preview.
        float speed = combatantData.SimStats?.GetStatValue(StatType.Speed) ?? combatantData.Stats.GetStatValue(StatType.Speed);
        if (speed <= 0) return; // Should not happen for an active combatant.

        // Calculate how many ticks passed for this turn to happen.
        float ticksToTurn = (TickThreshold - combatantData.Counter) / speed;

        // Advance all combatants' counters by the time that has passed.
        foreach (var c in combatantList)
        {
            // Use the appropriate stats (sim or real) for each combatant.
            float combatantSpeed = c.SimStats?.GetStatValue(StatType.Speed) ?? c.Stats.GetStatValue(StatType.Speed);
            c.Counter += combatantSpeed * ticksToTurn;
        }

        // The acting combatant "spends" the threshold and the action cost.
        // A higher action cost results in a lower starting counter for the next turn cycle.
        // Formula: NewCounter = OldCounter - 1000 (Base Turn) - ActionCost (Extra Delay)
        combatantData.Counter = combatantData.Counter - TickThreshold - actionTickCost;
    }
}
