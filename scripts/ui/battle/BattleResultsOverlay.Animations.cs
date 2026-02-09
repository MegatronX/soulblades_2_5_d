using Godot;
using System.Threading.Tasks;

public partial class BattleResultsOverlay
{
    private async Task PlayVictoryIntroAsync(BattleRewards rewards)
    {
        if (_backdrop != null)
        {
            var color = _backdrop.Color;
            color.A = 0f;
            _backdrop.Color = color;
        }

        if (_panel != null)
        {
            _panel.Position = _panelBasePosition + new Vector2(0, _introSlidePixels);
            _panel.Modulate = new Color(1, 1, 1, 0);
        }

        SetAlpha(_titleLabel, 0f);
        SetAlpha(_rewardsSummaryPanel, 0f);
        SetAlpha(_rewardsItemsPanel, 0f);
        SetAlpha(_buttonContainer, 0f);

        var introTween = CreateTween();
        if (_backdrop != null)
        {
            introTween.TweenProperty(_backdrop, "color:a", 0.6f, _introFadeSeconds);
        }
        if (_panel != null)
        {
            introTween.Parallel().TweenProperty(_panel, "modulate:a", 1f, _introFadeSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            introTween.Parallel().TweenProperty(_panel, "position", _panelBasePosition, _introSlideSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        }
        await ToSignal(introTween, Tween.SignalName.Finished);

        await FadeInAsync(_titleLabel, _introStaggerSeconds);
        await FadeInAsync(_rewardsSummaryPanel, _introStaggerSeconds);
        await FadeInAsync(_rewardsItemsPanel, _introStaggerSeconds);
        await FadeInAsync(_buttonContainer, _introStaggerSeconds);

        PulsePanel(_rewardsSummaryPanel);
        PulsePanel(_rewardsItemsPanel);

        if (_rewardsSummaryLabel != null)
        {
            await AnimateRewardCountsAsync(rewards);
        }
    }

    private async Task FadeInAsync(CanvasItem item, float delay)
    {
        if (item == null) return;
        item.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(item, "modulate:a", 1f, _introFadeSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
        if (delay > 0f)
        {
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private void SetAlpha(CanvasItem item, float alpha)
    {
        if (item == null) return;
        var c = item.Modulate;
        c.A = alpha;
        item.Modulate = c;
    }

    private void PulsePanel(CanvasItem panel)
    {
        if (panel == null) return;
        var tween = CreateTween();
        tween.TweenProperty(panel, "modulate", new Color(1.1f, 1.1f, 1.1f, panel.Modulate.A), _panelPulseSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(panel, "modulate", new Color(1f, 1f, 1f, panel.Modulate.A), _panelPulseSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    }
}
