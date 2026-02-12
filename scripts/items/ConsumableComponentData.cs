using Godot;

/// <summary>
/// A component that defines an item as "consumable," triggering a specific action when used.
/// </summary>
[GlobalClass]
public partial class ConsumableComponentData : ItemComponentData
{
    [Export(PropertyHint.ResourceType, "ActionData")]
    public ActionData ActionToPerform { get; private set; }

    [Export]
    public bool UsableInBattle { get; private set; } = true;

    [Export]
    public bool UsableInMenu { get; private set; } = true;

    [Export]
    public bool AlwaysUsableAsAction { get; private set; } = false;

    [Export]
    public ItemActionRequirements ActionUseRequirements { get; private set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float ActionConsumeChancePercent { get; private set; } = 100f;
}
