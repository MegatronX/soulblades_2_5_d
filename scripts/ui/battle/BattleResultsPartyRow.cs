using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class BattleResultsPartyRow : Control
{
    [Export] private NodePath _nameLabelPath;
    [Export] private NodePath _levelLabelPath;
    [Export] private NodePath _expLabelPath;
    [Export] private NodePath _expBarPath;
    [Export] private NodePath _portraitRectPath;
    [Export] private NodePath _bannerContainerPath;
    [Export] private NodePath _bannerLabelPath;
    [Export] private float _bannerDisplaySeconds = 1.2f;
    [Export] private float _bannerFadeSeconds = 0.15f;

    public Label NameLabel { get; private set; }
    public Label LevelLabel { get; private set; }
    public Label ExpLabel { get; private set; }
    public ProgressBar ExpBar { get; private set; }
    public TextureRect PortraitRect { get; private set; }
    public Control BannerContainer { get; private set; }
    public Label BannerLabel { get; private set; }
    public RichTextLabel BannerRichLabel { get; private set; }

    public bool HasRequiredNodes =>
        NameLabel != null &&
        LevelLabel != null &&
        ExpLabel != null &&
        ExpBar != null;

    private readonly Queue<string> _bannerQueue = new();
    private bool _bannerPlaying;
    private bool _warnedMissingBanner;

    public override void _Ready()
    {
        CacheNodes();
    }

    public void CacheNodes()
    {
        NameLabel = GetNodeOrNull<Label>(_nameLabelPath);
        LevelLabel = GetNodeOrNull<Label>(_levelLabelPath);
        ExpLabel = GetNodeOrNull<Label>(_expLabelPath);
        ExpBar = GetNodeOrNull<ProgressBar>(_expBarPath);
        PortraitRect = GetNodeOrNull<TextureRect>(_portraitRectPath);
        BannerContainer = GetNodeOrNull<Control>(_bannerContainerPath);
        BannerRichLabel = GetNodeOrNull<RichTextLabel>(_bannerLabelPath);
        BannerLabel = BannerRichLabel == null ? GetNodeOrNull<Label>(_bannerLabelPath) : null;

        if (!HasRequiredNodes)
        {
            GD.PrintErr("BattleResultsPartyRow: Missing required nodes (NameLabel, LevelLabel, ExpLabel, ExpBar).");
        }

        if (BannerContainer != null)
        {
            BannerContainer.Visible = false;
        }
    }

    public void EnqueueBanner(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _bannerQueue.Enqueue(text);
        if (!_bannerPlaying)
        {
            _ = PlayBannerQueueAsync();
        }
    }

    private async Task PlayBannerQueueAsync()
    {
        _bannerPlaying = true;

        while (_bannerQueue.Count > 0)
        {
            if (BannerContainer == null || (BannerLabel == null && BannerRichLabel == null))
            {
                if (!_warnedMissingBanner)
                {
                    GD.PrintErr("BattleResultsPartyRow: BannerContainer/BannerLabel not assigned.");
                    _warnedMissingBanner = true;
                }
                _bannerQueue.Clear();
                break;
            }

            string text = _bannerQueue.Dequeue();
            if (BannerRichLabel != null)
            {
                BannerRichLabel.Text = text;
            }
            else if (BannerLabel != null)
            {
                BannerLabel.Text = text;
            }
            BannerContainer.Modulate = new Color(1, 1, 1, 0);
            BannerContainer.Visible = true;

            var tween = CreateTween();
            tween.TweenProperty(BannerContainer, "modulate:a", 1.0f, _bannerFadeSeconds)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
            tween.TweenInterval(Mathf.Max(0.05f, _bannerDisplaySeconds));
            tween.TweenProperty(BannerContainer, "modulate:a", 0.0f, _bannerFadeSeconds)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);

            await ToSignal(tween, Tween.SignalName.Finished);
            BannerContainer.Visible = false;
        }

        _bannerPlaying = false;
    }
}
