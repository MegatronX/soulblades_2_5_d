using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// A component that manages all active status effects on a character.
/// It handles applying, removing, and ticking down the effects' durations,
/// and serves as the point of contact for the ActionDirector.
/// </summary>
[GlobalClass]
public partial class StatusEffectManager : Node
{
    public const string NodeName = "StatusEffectManager";

    [Signal]
    public delegate void StatusEffectAppliedEventHandler(StatusEffect effectData, Node owner);

    [Signal]
    public delegate void StatusEffectRemovedEventHandler(StatusEffect effectData, Node owner);

    // Represents an active instance of a status effect on this character.
    public class StatusEffectInstance
    {
        public StatusEffect EffectData { get; }
        public int RemainingTurns { get; set; }

        public StatusEffectInstance(StatusEffect effectData)
        {
            EffectData = effectData;
            // In a real game, you might use a shared RNG instance passed in.
            RemainingTurns = new RandomNumberGenerator().RandiRange(effectData.MinDurationTurns, effectData.MaxDurationTurns);
        }
    }

    private readonly List<StatusEffectInstance> _activeEffects = new();
    private Node _owner;

    public override void _Ready()
    {
        _owner = GetParent();
    }

    /// <summary>
    /// Applies a new status effect to this character and triggers its OnApply logic.
    /// </summary>
    public void ApplyEffect(StatusEffect effectData, ActionDirector actionDirector)
    {
        var instance = new StatusEffectInstance(effectData);
        _activeEffects.Add(instance);
        instance.EffectData.OnApply(_owner, actionDirector);
        GD.Print($"{_owner.Name} is now affected by {effectData.EffectName} for {instance.RemainingTurns} turns.");
        EmitSignal(SignalName.StatusEffectApplied, effectData, this.GetParent());
    }

    /// <summary>
    /// Removes a specific status effect instance and triggers its OnRemove logic.
    /// </summary>
    public void RemoveEffect(StatusEffectInstance instance, ActionDirector actionDirector)
    {
        instance.EffectData.OnRemove(_owner, actionDirector);
        _activeEffects.Remove(instance);
        GD.Print($"{instance.EffectData.EffectName} has worn off for {_owner.Name}.");
        EmitSignal(SignalName.StatusEffectRemoved, instance.EffectData, this.GetParent());
    }

    /// <summary>
    /// Called by the TurnManager at the start of this character's turn.
    /// Triggers all active OnTurnStart effects.
    /// </summary>
    public void OnTurnStart(ActionDirector actionDirector)
    {
        foreach (var instance in _activeEffects)
        {
            instance.EffectData.OnTurnStart(_owner, actionDirector);
        }
    }

    /// <summary>
    /// Called by the TurnManager at the end of this character's turn.
    /// Ticks down all active effects and removes any that have expired.
    /// </summary>
    public void OnTurnEnd(ActionDirector actionDirector)
    {
        // Check if we are currently "Stopped".
        bool isTurnSkipped = HasTurnSkippingEffect();

        // We iterate backwards because we may be removing items from the list.
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var instance = _activeEffects[i];
            
            // If the character is stopped, ONLY process effects that are causing the stop.
            // Pause all other effects (e.g. Regen, Poison).
            if (isTurnSkipped && !instance.EffectData.IsTurnSkipping)
            {
                continue;
            }

            // Trigger the effect's own end-of-turn logic (e.g., for Regen/Poison).
            instance.EffectData.OnTurnEnd(_owner, actionDirector);

            instance.RemainingTurns--;
            if (instance.RemainingTurns <= 0)
            {
                RemoveEffect(instance, actionDirector);
            }
        }
    }

    /// <summary>
    /// Called by the ActionDirector to get all action-modifying effects currently active on this character.
    /// </summary>
    /// <returns>A collection of active effects that can modify actions.</returns>
    public IEnumerable<IActionModifier> GetActionModifiers()
    {
        // This uses LINQ to select the EffectData from each active instance.
        // Since StatusEffect implements IActionModifier, this works seamlessly.
        return _activeEffects.Select(instance => instance.EffectData);
    }

    /// <summary>
    /// Checks if any active effect prevents the character from taking turns.
    /// </summary>
    public bool HasTurnSkippingEffect()
    {
        return _activeEffects.Any(e => e.EffectData.IsTurnSkipping);
    }

    /// <summary>
    /// Returns a read-only list of active effects. Used by TurnManager for simulation.
    /// </summary>
    public IReadOnlyList<StatusEffectInstance> GetActiveEffects()
    {
        return _activeEffects;
    }
}