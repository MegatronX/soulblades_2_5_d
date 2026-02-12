using Godot;
using System.Collections.Generic;
using System.Linq;

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

    [ExportGroup("Resistances")]
    [Export]
    public Godot.Collections.Dictionary<string, int> StatusEffectResistances { get; private set; } = new();

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
    public bool ApplyEffect(StatusEffect effectData, ActionDirector actionDirector)
    {
        return TryApplyEffect(effectData, actionDirector, 100f, null);
    }

    public bool TryApplyEffect(StatusEffect effectData, ActionDirector actionDirector, float baseChancePercent = 100f, IRandomNumberGenerator rng = null)
    {
        if (effectData == null || _owner == null) return false;

        if (HandleCancelEffects(effectData, actionDirector))
        {
            return false;
        }

        RemoveReplacementEffects(effectData, actionDirector);

        float resistance = GetResistance(effectData);
        float effectiveChance = Mathf.Clamp(baseChancePercent, 0f, 100f) * (1f - resistance / 100f);
        effectiveChance = Mathf.Clamp(effectiveChance, 0f, 100f);

        if (effectiveChance <= 0f) return false;

        if (effectiveChance < 100f)
        {
            float roll = rng != null ? rng.RandRangeFloat(0f, 100f) : GD.Randf() * 100f;
            if (roll > effectiveChance) return false;
        }

        var instance = new StatusEffectInstance(effectData);
        _activeEffects.Add(instance);
        instance.EffectData.OnApply(_owner, actionDirector);
        GD.Print($"{_owner.Name} is now affected by {effectData.EffectName} for {instance.RemainingTurns} turns.");
        EmitSignal(SignalName.StatusEffectApplied, effectData, this.GetParent());
        return true;
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

    private bool HandleCancelEffects(StatusEffect effectData, ActionDirector actionDirector)
    {
        if (effectData.CancelEffects == null || effectData.CancelEffects.Count == 0) return false;

        bool removedAny = false;
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var active = _activeEffects[i];
            if (IsEffectInList(active.EffectData, effectData.CancelEffects))
            {
                RemoveEffect(active, actionDirector);
                removedAny = true;
            }
        }

        return removedAny;
    }

    private void RemoveReplacementEffects(StatusEffect effectData, ActionDirector actionDirector)
    {
        if (effectData.ReplacementEffects == null || effectData.ReplacementEffects.Count == 0) return;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var active = _activeEffects[i];
            if (IsEffectInList(active.EffectData, effectData.ReplacementEffects))
            {
                RemoveEffect(active, actionDirector);
            }
        }
    }

    private int GetResistance(StatusEffect effectData)
    {
        if (effectData == null) return 0;
        var key = ResolveResistanceKey(effectData);
        return GetResistance(key);
    }

    public int GetResistance(string effectKey)
    {
        if (string.IsNullOrEmpty(effectKey)) return 0;
        return Mathf.Clamp(StatusEffectResistances.GetValueOrDefault(effectKey, 0), 0, 100);
    }

    public void SetResistance(string effectKey, int value)
    {
        if (string.IsNullOrEmpty(effectKey)) return;
        StatusEffectResistances[effectKey] = Mathf.Clamp(value, 0, 100);
    }

    public void AddResistance(string effectKey, int delta)
    {
        if (string.IsNullOrEmpty(effectKey)) return;
        int current = GetResistance(effectKey);
        StatusEffectResistances[effectKey] = Mathf.Clamp(current + delta, 0, 100);
    }

    public void RemoveResistance(string effectKey, int delta)
    {
        AddResistance(effectKey, -delta);
    }

    public void ClearResistance(string effectKey)
    {
        if (string.IsNullOrEmpty(effectKey)) return;
        StatusEffectResistances.Remove(effectKey);
    }

    public int GetResistance(StatusEffect effectData, bool useNameFallback)
    {
        if (effectData == null) return 0;
        var key = ResolveResistanceKey(effectData, useNameFallback);
        return GetResistance(key);
    }

    public void AddResistance(StatusEffect effectData, int delta, bool useNameFallback = true)
    {
        var key = ResolveResistanceKey(effectData, useNameFallback);
        AddResistance(key, delta);
    }

    public void RemoveResistance(StatusEffect effectData, int delta, bool useNameFallback = true)
    {
        var key = ResolveResistanceKey(effectData, useNameFallback);
        RemoveResistance(key, delta);
    }

    public void SetResistance(StatusEffect effectData, int value, bool useNameFallback = true)
    {
        var key = ResolveResistanceKey(effectData, useNameFallback);
        SetResistance(key, value);
    }

    public void ApplyResistanceChanges(IEnumerable<KeyValuePair<string, int>> changes)
    {
        if (changes == null) return;
        foreach (var change in changes)
        {
            AddResistance(change.Key, change.Value);
        }
    }

    public void ApplyResistanceChanges(IEnumerable<StatusEffectChanceEntry> changes)
    {
        if (changes == null) return;
        foreach (var change in changes)
        {
            if (change?.Effect == null) continue;
            AddResistance(change.Effect, Mathf.RoundToInt(change.ChancePercent));
        }
    }

    private static string ResolveResistanceKey(StatusEffect effectData, bool useNameFallback = true)
    {
        if (effectData == null) return null;

        if (!string.IsNullOrEmpty(effectData.ResourcePath))
        {
            return effectData.ResourcePath;
        }

        if (useNameFallback)
        {
            if (!string.IsNullOrEmpty(effectData.EffectName)) return effectData.EffectName;
            if (!string.IsNullOrEmpty(effectData.ResourceName)) return effectData.ResourceName;
        }

        return null;
    }

    private static bool IsEffectInList(StatusEffect effectData, Godot.Collections.Array<StatusEffect> list)
    {
        if (effectData == null || list == null || list.Count == 0) return false;

        foreach (var candidate in list)
        {
            if (candidate == null) continue;
            if (AreSameEffect(effectData, candidate)) return true;
        }
        return false;
    }

    private static bool AreSameEffect(StatusEffect a, StatusEffect b)
    {
        if (a == null || b == null) return false;
        if (a == b) return true;

        var aPath = a.ResourcePath;
        var bPath = b.ResourcePath;
        if (!string.IsNullOrEmpty(aPath) && !string.IsNullOrEmpty(bPath))
        {
            return aPath == bPath;
        }

        if (!string.IsNullOrEmpty(a.EffectName) && !string.IsNullOrEmpty(b.EffectName))
        {
            return string.Equals(a.EffectName, b.EffectName, System.StringComparison.OrdinalIgnoreCase);
        }

        return false;
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
