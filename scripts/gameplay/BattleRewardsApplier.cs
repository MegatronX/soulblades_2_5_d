using Godot;
using System.Collections.Generic;

public sealed class BattleRewardsApplier
{
    public void Apply(BattleRewards rewards, IReadOnlyList<Node> party, InventoryManager inventory, GameManager gameManager, BattleConfig config = null, Node killingBlowActor = null)
    {
        if (rewards == null) return;

        foreach (var kvp in rewards.Items)
        {
            inventory?.AddItem(kvp.Key, kvp.Value);
        }

        gameManager?.AddPartyMoney(rewards.TotalMoney);
        gameManager?.AddPartyExperience(rewards.TotalExperience);

        if (party == null || party.Count == 0) return;

        bool splitExp = config?.SplitExperienceAcrossParty ?? true;
        bool splitAp = config?.SplitApAcrossParty ?? true;
        float bonusPercent = Mathf.Max(0f, config?.KillingBlowExpBonusPercent ?? 0f);

        int totalExp = rewards.TotalExperience;
        int totalAp = rewards.TotalApExperience;

        int perMemberExp = splitExp ? totalExp / party.Count : totalExp;
        int expRemainder = splitExp ? totalExp % party.Count : 0;
        int perMemberAp = splitAp ? totalAp / party.Count : totalAp;
        int apRemainder = splitAp ? totalAp % party.Count : 0;

        for (int i = 0; i < party.Count; i++)
        {
            var member = party[i];
            int expToGive = perMemberExp + ((splitExp && i == 0) ? expRemainder : 0);
            int apToGive = perMemberAp + ((splitAp && i == 0) ? apRemainder : 0);

            if (bonusPercent > 0f && killingBlowActor != null && member == killingBlowActor)
            {
                int bonusExp = Mathf.RoundToInt(totalExp * (bonusPercent / 100f));
                if (bonusExp > 0)
                {
                    expToGive += bonusExp;
                }
            }

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
