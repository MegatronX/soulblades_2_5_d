using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages a character's elemental affinities and resistances.
/// Separating this from StatsComponent keeps the logic clean, as resistances
/// often follow different rules (percentage-based, caps) than primary stats.
/// </summary>
[GlobalClass]
public partial class ElementalComponent : Node
{
    public const string NodeName = "ElementalComponent";

    [Export]
    private ElementalProfile _elementalProfile;

    // 1.0 = Normal, >1.0 = Weak, <1.0 = Resist, 0 = Immune, <0 = Absorb
    private Dictionary<ElementType, float> _resistances = new();

    public override void _Ready()
    {
        InitializeResistances();
    }

    public void SetElementalProfile(ElementalProfile profile)
    {
        _elementalProfile = profile;
        InitializeResistances();
    }

    public float GetResistanceMultiplier(ElementType element)
    {
        return _resistances.GetValueOrDefault(element, 1.0f);
    }

    public void SetResistance(ElementType element, float multiplier)
    {
        _resistances[element] = multiplier;
    }

    private void InitializeResistances()
    {
        _resistances.Clear();
        if (_elementalProfile?.ElementalResistances != null)
        {
            foreach (var kvp in _elementalProfile.ElementalResistances)
            {
                _resistances[kvp.Key] = kvp.Value;
            }
        }
    }
}