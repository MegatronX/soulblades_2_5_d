using Godot;

/// <summary>
/// Base class for all Ability Resources. An ability is a data-driven object
/// that can contain logic for modifying actions via the IActionModifier interface.
/// </summary>
[GlobalClass]
public partial class AbilityData : Resource, IActionModifier
{
    // By having the base resource implement the interface, all derived
    // ability types automatically become action modifiers.
}