using Godot;

/// <summary>
/// Defines the elemental affinities and resistances for a character.
/// Separated from BaseStats to allow for modular assignment (e.g. "Fire Type" profile).
/// </summary>
[GlobalClass]
public partial class ElementalProfile : Resource
{
    // 1.0 = Normal, >1.0 = Weak, <1.0 = Resist, 0 = Immune, <0 = Absorb
    [Export]
    public Godot.Collections.Dictionary<ElementType, float> ElementalResistances { get; set; } = new();
}