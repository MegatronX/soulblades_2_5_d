using System.Collections.Generic;

public sealed class BattleRewards
{
    public int TotalExperience { get; set; }
    public int TotalApExperience { get; set; }
    public int TotalMoney { get; set; }

    public Dictionary<ItemData, int> Items { get; } = new();

    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;
        if (Items.ContainsKey(item))
        {
            Items[item] += quantity;
        }
        else
        {
            Items[item] = quantity;
        }
    }
}
