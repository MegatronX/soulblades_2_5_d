using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class BattleResultsPartyRow : Control
{
    [Export] private NodePath _nameLabelPath;
    [Export] private NodePath _levelLabelPath;
    [Export] private NodePath _expLabelPath;
    [Export] private NodePath _expGainLabelPath;
    [Export] private NodePath _expBarPath;
    [Export] private NodePath _portraitRectPath;
    [Export] private NodePath _bannerContainerPath;
    [Export] private NodePath _bannerLabelPath;
    [Export] private NodePath _bannerAudioPath;
    [Export] private NodePath _bannerSparkleLayerPath;
    [Export] private BannerEffectConfig _bannerEffects;

    public Label NameLabel { get; private set; }
    public Label LevelLabel { get; private set; }
    public Label ExpLabel { get; private set; }
    public Label ExpGainLabel { get; private set; }
    public ProgressBar ExpBar { get; private set; }
    public TextureRect PortraitRect { get; private set; }
    public Control BannerContainer { get; private set; }
    public Label BannerLabel { get; private set; }
    public RichTextLabel BannerRichLabel { get; private set; }
    public AudioStreamPlayer BannerAudio { get; private set; }
    public Control BannerSparkleLayer { get; private set; }

    public bool HasRequiredNodes =>
        NameLabel != null &&
        LevelLabel != null &&
        ExpLabel != null &&
        ExpBar != null;

    private readonly List<string> _bannerQueue = new();
    private bool _bannerPlaying;
    private bool _warnedMissingBanner;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        CacheNodes();
    }

    public void CacheNodes()
    {
        NameLabel = GetNodeOrNull<Label>(_nameLabelPath);
        LevelLabel = GetNodeOrNull<Label>(_levelLabelPath);
        ExpLabel = GetNodeOrNull<Label>(_expLabelPath);
        ExpGainLabel = GetNodeOrNull<Label>(_expGainLabelPath);
        ExpBar = GetNodeOrNull<ProgressBar>(_expBarPath);
        PortraitRect = GetNodeOrNull<TextureRect>(_portraitRectPath);
        BannerContainer = GetNodeOrNull<Control>(_bannerContainerPath);
        BannerRichLabel = GetNodeOrNull<RichTextLabel>(_bannerLabelPath);
        BannerLabel = BannerRichLabel == null ? GetNodeOrNull<Label>(_bannerLabelPath) : null;
        BannerAudio = GetNodeOrNull<AudioStreamPlayer>(_bannerAudioPath);
        BannerSparkleLayer = GetNodeOrNull<Control>(_bannerSparkleLayerPath);

        if (!HasRequiredNodes)
        {
            GD.PrintErr("BattleResultsPartyRow: Missing required nodes (NameLabel, LevelLabel, ExpLabel, ExpBar).");
        }

        if (BannerContainer != null)
        {
            BannerContainer.Visible = false;
        }
    }

    public void EnqueueBanner(string text, bool priority = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (priority)
        {
            _bannerQueue.Insert(0, text);
        }
        else
        {
            _bannerQueue.Add(text);
        }
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

            string text = _bannerQueue[0];
            _bannerQueue.RemoveAt(0);
            if (BannerRichLabel != null)
            {
                BannerRichLabel.Text = text;
            }
            else if (BannerLabel != null)
            {
                BannerLabel.Text = text;
            }
            var fx = _bannerEffects ?? BannerEffectConfig.Default;
            BannerContainer.Scale = Vector2.One * fx.BannerPopStartScale;
            BannerContainer.Modulate = new Color(1, 1, 1, 0);
            BannerContainer.Visible = true;

            if (fx.PlayBannerSound && BannerAudio?.Stream != null)
            {
                BannerAudio.Play();
            }

            var bannerLabel = (CanvasItem)BannerRichLabel ?? BannerLabel;
            if (bannerLabel != null)
            {
                bannerLabel.Modulate = fx.BannerGlowColor;
            }
            await SpawnBannerSparklesAsync(fx);

            var tween = CreateTween();
            tween.TweenProperty(BannerContainer, "modulate:a", 1.0f, fx.BannerFadeSeconds)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
            tween.Parallel().TweenProperty(BannerContainer, "scale", Vector2.One * fx.BannerPopScale, fx.BannerPopSeconds)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(BannerContainer, "scale", Vector2.One, fx.BannerPopSeconds)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
            if (bannerLabel != null)
            {
                tween.Parallel().TweenProperty(bannerLabel, "modulate", Colors.White, fx.BannerGlowSeconds)
                    .SetTrans(Tween.TransitionType.Sine)
                    .SetEase(Tween.EaseType.Out);
            }
            tween.TweenInterval(Mathf.Max(0.05f, fx.BannerDisplaySeconds));
            tween.TweenProperty(BannerContainer, "modulate:a", 0.0f, fx.BannerFadeSeconds)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);

            await ToSignal(tween, Tween.SignalName.Finished);
            BannerContainer.Visible = false;
        }

        _bannerPlaying = false;
    }

    private async Task SpawnBannerSparklesAsync(BannerEffectConfig fx)
    {
        if (fx == null || !fx.PlayBannerSparkles || BannerContainer == null) return;

        var sparkleHost = BannerSparkleLayer ?? BannerContainer;
        if (sparkleHost == null) return;

        Rect2 bannerRect = BannerContainer?.GetGlobalRect() ?? new Rect2(Vector2.Zero, Vector2.Zero);
        if (bannerRect.Size == Vector2.Zero)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            bannerRect = BannerContainer?.GetGlobalRect() ?? new Rect2(Vector2.Zero, Vector2.Zero);
        }

        Vector2 hostOrigin = Vector2.Zero;
        if (bannerRect.Size != Vector2.Zero)
        {
            var inv = sparkleHost.GetGlobalTransformWithCanvas().AffineInverse();
            hostOrigin = inv * bannerRect.Position;
        }

        Vector2 size = bannerRect.Size;
        if (size.X <= 1f || size.Y <= 1f)
        {
            size = BannerContainer.GetRect().Size;
        }
        if (size.X <= 1f || size.Y <= 1f)
        {
            size = new Vector2(220, 48);
        }

        float inset = Mathf.Max(0f, fx.BannerSparkleInset);
        float safeWidth = Mathf.Max(0f, size.X - inset * 2f);
        float safeHeight = Mathf.Max(0f, size.Y - inset * 2f);

        int count = Mathf.Max(0, fx.BannerSparkleCount);
        for (int i = 0; i < count; i++)
        {
            Control sparkle;
            if (fx.BannerSparkleTexture != null)
            {
                var rect = new TextureRect
                {
                    Texture = fx.BannerSparkleTexture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    ZIndex = 1
                };
                rect.Modulate = fx.BannerSparkleColor;
                sparkle = rect;
            }
            else
            {
                var rect = new ColorRect
                {
                    Color = fx.BannerSparkleColor,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    ZIndex = 1
                };
                sparkle = rect;
            }

            float width = _rng.RandfRange(fx.BannerSparkleSizeMin.X, fx.BannerSparkleSizeMax.X);
            float height = _rng.RandfRange(fx.BannerSparkleSizeMin.Y, fx.BannerSparkleSizeMax.Y);
            sparkle.Size = new Vector2(width, height);
            sparkle.PivotOffset = sparkle.Size * 0.5f;
            sparkle.Rotation = Mathf.DegToRad(_rng.RandfRange(-35f, 35f));
            sparkle.Scale = Vector2.One * fx.BannerSparkleStartScale;
            sparkle.Modulate = new Color(sparkle.Modulate.R, sparkle.Modulate.G, sparkle.Modulate.B, 0);

            float x = inset + _rng.RandfRange(0, Mathf.Max(0, safeWidth - sparkle.Size.X));
            float y = inset + _rng.RandfRange(0, Mathf.Max(0, safeHeight - sparkle.Size.Y));
            sparkle.Position = hostOrigin + new Vector2(x, y);

            sparkleHost.AddChild(sparkle);

            float maxX = hostOrigin.X + inset + Mathf.Max(0, safeWidth - sparkle.Size.X);
            float maxY = hostOrigin.Y + inset + Mathf.Max(0, safeHeight - sparkle.Size.Y);
            float minX = hostOrigin.X + inset;
            float minY = hostOrigin.Y + inset;

            float currentX = hostOrigin.X + x;
            float currentY = hostOrigin.Y + y;
            float driftXMax = Mathf.Min(fx.BannerSparkleDriftX, maxX - currentX);
            float driftXMin = Mathf.Max(-fx.BannerSparkleDriftX, minX - currentX);
            float driftX = _rng.RandfRange(driftXMin, driftXMax);

            float targetY = Mathf.Clamp(currentY + fx.BannerSparkleDriftY, minY, maxY);
            float driftY = targetY - currentY;

            var drift = new Vector2(driftX, driftY);
            var tween = CreateTween();
            tween.TweenProperty(sparkle, "modulate:a", 1f, fx.BannerSparkleFadeInSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.Parallel().TweenProperty(sparkle, "scale", Vector2.One, fx.BannerSparkleFadeInSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.Parallel().TweenProperty(sparkle, "position", sparkle.Position + drift, fx.BannerSparkleFadeInSeconds + fx.BannerSparkleHoldSeconds + fx.BannerSparkleFadeOutSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            if (fx.BannerSparkleHoldSeconds > 0f)
            {
                tween.TweenInterval(fx.BannerSparkleHoldSeconds);
            }
            tween.TweenProperty(sparkle, "modulate:a", 0f, fx.BannerSparkleFadeOutSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
            tween.TweenCallback(Callable.From(sparkle.QueueFree));
        }
    }
}
