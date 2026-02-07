using Godot;

/// <summary>
/// Properties attached to an enemy for post-battle rewards and loot.
/// </summary>
[GlobalClass]
public partial class EnemyProperties : Node
{
    public const string NodeName = "EnemyProperties";

    [Export]
    public Godot.Collections.Array<LootEntry> StealTable { get; set; } = new();

    [Export]
    public Godot.Collections.Array<LootEntry> DropTable { get; set; } = new();

    [Export]
    public bool AllowMultipleSteals { get; set; } = false;

    [Export]
    public bool AllowMultipleDrops { get; set; } = false;

    [Export]
    public bool AllowDropAfterSteal { get; set; } = true;

    [Export(PropertyHint.Range, "0,1000000,1")]
    public int ExperienceReward { get; set; } = 0;

    [Export(PropertyHint.Range, "0,1000000,1")]
    public int APExperienceReward { get; set; } = 0;

    [Export(PropertyHint.Range, "0,1000000,1")]
    public int MoneyReward { get; set; } = 0;

    public System.Collections.Generic.HashSet<ItemData> StolenItems { get; } = new();

    public bool HasSuccessfulSteal => StolenItems.Count > 0;

    public bool TryRecordSteal(ItemData item)
    {
        if (item == null) return false;
        if (!AllowMultipleSteals && StolenItems.Count > 0) return false;
        return StolenItems.Add(item);
    }
}
