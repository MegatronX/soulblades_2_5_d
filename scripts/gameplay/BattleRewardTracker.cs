using Godot;
using System.Collections.Generic;

public sealed class BattleRewardTracker
{
    private readonly BattleRewardsResolver _resolver = new BattleRewardsResolver();
    private readonly List<EnemyRewardSnapshot> _defeatedEnemyRewards = new();
    private readonly HashSet<ulong> _rewardedEnemyIds = new();

    public void RegisterDefeat(Node combatant, System.Func<Node, bool> isPlayerSide = null)
    {
        if (combatant == null) return;
        if (isPlayerSide != null && isPlayerSide(combatant)) return;

        ulong id = combatant.GetInstanceId();
        if (_rewardedEnemyIds.Contains(id)) return;

        var props = combatant.GetNodeOrNull<EnemyProperties>(EnemyProperties.NodeName);
        if (props == null)
        {
            foreach (var child in combatant.GetChildren())
            {
                if (child is EnemyProperties ep)
                {
                    props = ep;
                    break;
                }
            }
        }

        if (props == null) return;

        _defeatedEnemyRewards.Add(EnemyRewardSnapshot.From(props));
        _rewardedEnemyIds.Add(id);
    }

    public BattleRewards ResolveRewards(IRandomNumberGenerator rng)
    {
        return _resolver.ResolveRewards(_defeatedEnemyRewards, rng);
    }

    public void Reset()
    {
        _defeatedEnemyRewards.Clear();
        _rewardedEnemyIds.Clear();
    }
}
