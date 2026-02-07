using Godot;
using System.Collections.Generic;

public sealed class BattleRewardsApplier
{
    public void Apply(BattleRewards rewards, IReadOnlyList<Node> party, InventoryManager inventory, GameManager gameManager)
    {
        if (rewards == null) return;

        foreach (var kvp in rewards.Items)
        {
            inventory?.AddItem(kvp.Key, kvp.Value);
        }

        gameManager?.AddPartyMoney(rewards.TotalMoney);
        gameManager?.AddPartyExperience(rewards.TotalExperience);

        if (party == null || party.Count == 0) return;

        int perMemberExp = rewards.TotalExperience / party.Count;
        int expRemainder = rewards.TotalExperience % party.Count;
        int perMemberAp = rewards.TotalApExperience / party.Count;
        int apRemainder = rewards.TotalApExperience % party.Count;

        for (int i = 0; i < party.Count; i++)
        {
            var member = party[i];
            int expToGive = perMemberExp + (i == 0 ? expRemainder : 0);
            int apToGive = perMemberAp + (i == 0 ? apRemainder : 0);

            var leveling = member.GetNodeOrNull<LevelingComponent>(LevelingComponent.NodeName);
            if (leveling != null)
            {
                leveling.AddExperience(expToGive);
            }
            else
            {
                member.GetNodeOrNull<ExperienceComponent>(ExperienceComponent.NodeName)?.AddExperience(expToGive);
            }
            member.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName)?.AddApExperience(apToGive);
        }
    }
}
