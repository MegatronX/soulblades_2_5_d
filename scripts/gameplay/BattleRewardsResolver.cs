using Godot;
using System.Collections.Generic;

public sealed class BattleRewardsResolver
{
    public BattleRewards ResolveRewards(IEnumerable<EnemyRewardSnapshot> defeatedEnemies, IRandomNumberGenerator rng)
    {
        var rewards = new BattleRewards();
        if (defeatedEnemies == null || rng == null) return rewards;

        foreach (var enemy in defeatedEnemies)
        {
            if (enemy == null) continue;

            rewards.TotalExperience += enemy.ExperienceReward;
            rewards.TotalApExperience += enemy.ApExperienceReward;
            rewards.TotalMoney += enemy.MoneyReward;

            if (!enemy.AllowDropAfterSteal && enemy.HasSuccessfulSteal)
            {
                continue;
            }

            var drops = RollTable(enemy.DropTable, enemy.AllowMultipleDrops, rng);
            foreach (var item in drops)
            {
                rewards.AddItem(item);
            }
        }

        return rewards;
    }

    private static List<ItemData> RollTable(List<LootEntry> table, bool allowMultiple, IRandomNumberGenerator rng)
    {
        var results = new List<ItemData>();
        if (table == null || rng == null) return results;

        foreach (var entry in table)
        {
            if (entry?.Item == null) continue;
            float roll = rng.RandRangeFloat(0f, 100f);
            if (roll <= entry.ChancePercent)
            {
                results.Add(entry.Item);
                if (!allowMultiple) break;
            }
        }

        return results;
    }
}
