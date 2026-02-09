using Godot;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class BattleResultsOverlay
{
    private static string BuildRewardsText(BattleRewards rewards)
    {
        if (rewards == null)
        {
            return "[center]No rewards.[/center]";
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center]");
        sb.AppendLine($"EXP: {rewards.TotalExperience}");
        sb.AppendLine($"AP: {rewards.TotalApExperience}");
        sb.AppendLine($"Money: {rewards.TotalMoney}");
        sb.AppendLine("");
        sb.AppendLine("Items:");

        if (rewards.Items.Count == 0)
        {
            sb.AppendLine("None");
        }
        else
        {
            foreach (var kvp in rewards.Items.OrderBy(k => k.Key.ItemName))
            {
                sb.AppendLine($"{kvp.Key.ItemName} x{kvp.Value}");
            }
        }

        sb.AppendLine("[/center]");

        return sb.ToString();
    }

    private static string BuildRewardsSummaryText(BattleRewards rewards)
    {
        if (rewards == null)
        {
            return "[center]No rewards.[/center]";
        }
        return BuildRewardsSummaryText(rewards.TotalExperience, rewards.TotalApExperience, rewards.TotalMoney);
    }

    private static string BuildRewardsSummaryText(int exp, int ap, int money)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[center]");
        sb.AppendLine($"EXP: {exp}");
        sb.AppendLine($"AP: {ap}");
        sb.AppendLine($"Money: {money}");
        sb.AppendLine("[/center]");
        return sb.ToString();
    }

    private static string BuildRewardsItemsText(BattleRewards rewards)
    {
        if (rewards == null)
        {
            return "[center]Items:\nNone[/center]";
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center]");
        sb.AppendLine("Items:");

        if (rewards.Items.Count == 0)
        {
            sb.AppendLine("None");
        }
        else
        {
            foreach (var kvp in rewards.Items.OrderBy(k => k.Key.ItemName))
            {
                sb.AppendLine($"{kvp.Key.ItemName} x{kvp.Value}");
            }
        }

        sb.AppendLine("[/center]");
        return sb.ToString();
    }

    private async Task AnimateRewardCountsAsync(BattleRewards rewards)
    {
        if (rewards == null) return;
        float elapsed = 0f;
        int exp = 0;
        int ap = 0;
        int money = 0;

        while (elapsed < _rewardCountSeconds)
        {
            float t = Mathf.Clamp(elapsed / _rewardCountSeconds, 0f, 1f);
            exp = Mathf.RoundToInt(Mathf.Lerp(0, rewards.TotalExperience, t));
            ap = Mathf.RoundToInt(Mathf.Lerp(0, rewards.TotalApExperience, t));
            money = Mathf.RoundToInt(Mathf.Lerp(0, rewards.TotalMoney, t));
            _rewardsSummaryLabel.Text = BuildRewardsSummaryText(exp, ap, money);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            elapsed += (float)GetProcessDeltaTime();
        }

        _rewardsSummaryLabel.Text = BuildRewardsSummaryText(rewards);
    }
}
