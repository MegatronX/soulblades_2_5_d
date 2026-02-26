using Godot;
using System.Collections.Generic;

/// <summary>
/// Holds and exposes active global battlefield effects.
/// Place this on the battle scene to drive weather/terrain style modifiers.
/// </summary>
[GlobalClass]
public partial class BattlefieldEffectManager : Node
{
    public const string NodeName = "BattlefieldEffectManager";

    [Signal]
    public delegate void EffectsChangedEventHandler();

    [Export]
    public Godot.Collections.Array<BattlefieldEffect> Effects { get; private set; } = new();

    public IReadOnlyList<BattlefieldEffect> GetEffects() => Effects;

    public IEnumerable<IActionModifier> GetActionModifiers()
    {
        foreach (var effect in Effects)
        {
            if (effect == null || !effect.IsActive) continue;
            yield return effect;
        }
    }

    public bool AddEffect(BattlefieldEffect effect)
    {
        if (effect == null || Effects.Contains(effect)) return false;
        Effects.Add(effect);
        EmitSignal(SignalName.EffectsChanged);
        return true;
    }

    public bool RemoveEffect(BattlefieldEffect effect)
    {
        if (effect == null) return false;
        bool removed = Effects.Remove(effect);
        if (removed)
        {
            EmitSignal(SignalName.EffectsChanged);
        }
        return removed;
    }

    public void ClearEffects()
    {
        if (Effects.Count == 0) return;
        Effects.Clear();
        EmitSignal(SignalName.EffectsChanged);
    }
}
