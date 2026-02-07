using System.Collections.Generic;

public sealed class EnemyRewardSnapshot
{
    public int ExperienceReward { get; set; }
    public int ApExperienceReward { get; set; }
    public int MoneyReward { get; set; }

    public bool AllowMultipleDrops { get; set; }
    public bool AllowDropAfterSteal { get; set; }
    public bool HasSuccessfulSteal { get; set; }

    public List<LootEntry> DropTable { get; set; } = new();

    public static EnemyRewardSnapshot From(EnemyProperties props)
    {
        var snapshot = new EnemyRewardSnapshot
        {
            ExperienceReward = props.ExperienceReward,
            ApExperienceReward = props.APExperienceReward,
            MoneyReward = props.MoneyReward,
            AllowMultipleDrops = props.AllowMultipleDrops,
            AllowDropAfterSteal = props.AllowDropAfterSteal,
            HasSuccessfulSteal = props.HasSuccessfulSteal
        };

        if (props.DropTable != null)
        {
            foreach (var entry in props.DropTable)
            {
                if (entry != null)
                {
                    snapshot.DropTable.Add(entry);
                }
            }
        }

        return snapshot;
    }
}
