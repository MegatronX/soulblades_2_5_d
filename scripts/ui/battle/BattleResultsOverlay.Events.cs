using Godot;
using System.Collections.Generic;

public partial class BattleResultsOverlay
{
    private void BindLevelEvents(IEnumerable<Node> partyMembers)
    {
        UnbindLevelEvents();
        if (_eventsLabel != null) _eventsLabel.Text = string.Empty;

        if (partyMembers == null) return;

        foreach (var member in partyMembers)
        {
            if (member == null) continue;
            var leveling = member.GetNodeOrNull<LevelingComponent>(LevelingComponent.NodeName);
            if (leveling == null) continue;

            string memberName = GetDisplayName(member);
            var memberRef = member;

            LevelingComponent.LevelUpEventHandler levelUpHandler = (oldLevel, newLevel) =>
            {
                var message = $"{memberName} reached Lv {newLevel}!";
                AppendEventLine(message);
                ShowRowBanner(memberRef, $"LEVEL UP! Lv {newLevel}");
                FlushLevelRewardBanners(memberRef);
            };
            leveling.LevelUp += levelUpHandler;
            _unbindLevelEvents.Add(() => leveling.LevelUp -= levelUpHandler);

            LevelingComponent.StatIncreasedEventHandler statHandler = (statType, oldValue, newValue) =>
            {
                var message = $"{memberName} {((StatType)statType)} {oldValue} -> {newValue}";
                BufferLevelRewardBanner(memberRef, message);
            };
            leveling.StatIncreased += statHandler;
            _unbindLevelEvents.Add(() => leveling.StatIncreased -= statHandler);

            LevelingComponent.AbilityLearnedEventHandler abilityHandler = ability =>
            {
                var message = $"{memberName} learned {ability.AbilityName}";
                BufferLevelRewardBanner(memberRef, message);
            };
            leveling.AbilityLearned += abilityHandler;
            _unbindLevelEvents.Add(() => leveling.AbilityLearned -= abilityHandler);

            LevelingComponent.ActionLearnedEventHandler actionHandler = action =>
            {
                var message = $"{memberName} learned {action.CommandName}";
                BufferLevelRewardBanner(memberRef, message);
            };
            leveling.ActionLearned += actionHandler;
            _unbindLevelEvents.Add(() => leveling.ActionLearned -= actionHandler);
        }
    }

    private void UnbindLevelEvents()
    {
        foreach (var unbind in _unbindLevelEvents)
        {
            unbind();
        }
        _unbindLevelEvents.Clear();
        _pendingLevelRewardBanners.Clear();
    }

    private void AppendEventLine(string text)
    {
        if (_eventsLabel == null) return;
        if (string.IsNullOrEmpty(_eventsLabel.Text))
        {
            _eventsLabel.Text = text;
        }
        else
        {
            _eventsLabel.Text += "\n" + text;
        }
    }

    private void BufferLevelRewardBanner(Node member, string text)
    {
        if (member == null || string.IsNullOrEmpty(text)) return;
        if (!_pendingLevelRewardBanners.TryGetValue(member, out var list))
        {
            list = new List<string>();
            _pendingLevelRewardBanners[member] = list;
        }
        list.Add(text);
    }

    private void FlushLevelRewardBanners(Node member)
    {
        if (member == null) return;
        if (!_pendingLevelRewardBanners.TryGetValue(member, out var list) || list.Count == 0)
        {
            return;
        }

        foreach (var text in list)
        {
            ShowRowBanner(member, text);
            AppendEventLine(text);
        }
        list.Clear();
    }

    private void ShowRowBanner(Node member, string text, bool priority = false)
    {
        if (member == null) return;
        if (_canShowBanners && _partyRowsByMember.TryGetValue(member, out var row) && row?.RowComponent != null)
        {
            row.RowComponent.EnqueueBanner(text, priority);
            return;
        }

        if (!_pendingRowBanners.TryGetValue(member, out var queue))
        {
            queue = new List<string>();
            _pendingRowBanners[member] = queue;
        }
        if (priority)
        {
            queue.Insert(0, text);
        }
        else
        {
            queue.Add(text);
        }
    }

    private void FlushPendingRowBanners()
    {
        if (_pendingRowBanners.Count == 0) return;

        foreach (var kvp in _pendingRowBanners)
        {
            if (!_partyRowsByMember.TryGetValue(kvp.Key, out var row) || row?.RowComponent == null)
            {
                continue;
            }

            foreach (var text in kvp.Value)
            {
                row.RowComponent.EnqueueBanner(text);
            }
        }

        _pendingRowBanners.Clear();
    }
}
