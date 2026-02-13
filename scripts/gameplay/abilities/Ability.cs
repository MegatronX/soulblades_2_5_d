using Godot;

/// <summary>
/// A Resource that defines a character Ability (e.g., "Last Chance", "Bracer").
/// It acts as a container for various EffectLogic components that define its behavior.
/// Unlike StatusEffects, Abilities are generally permanent as long as they are equipped.
/// </summary>
[GlobalClass]
public partial class Ability : Resource
{
    [Export]
    public string AbilityName { get; private set; }

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; }

    [Export]
    public int ApCost { get; private set; } = 1;

    [Export]
    public bool IsStackable { get; private set; } = false;

    [ExportGroup("Priority")]
    [Export]
    public int Priority { get; private set; } = 0;

    [Export]
    public Godot.Collections.Array<EffectLogic> Effects { get; private set; }

    [Export]
    public Godot.Collections.Array<AbilityEffect> TriggeredEffects { get; private set; } = new();
}
