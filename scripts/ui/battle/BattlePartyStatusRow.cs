using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattlePartyStatusRow : Control
{
    [Export] private NodePath _nameLabelPath;
    [Export] private NodePath _panelPath;
    [Export] private NodePath _hpBarPath;
    [Export] private NodePath _hpNameLabelPath;
    [Export] private NodePath _hpCurrentLabelPath;
    [Export] private NodePath _hpMaxLabelPath;
    [Export] private NodePath _mpBarPath;
    [Export] private NodePath _mpNameLabelPath;
    [Export] private NodePath _mpCurrentLabelPath;
    [Export] private NodePath _mpMaxLabelPath;
    [Export] private NodePath _chargeNotchContainerPath;
    [Export] private NodePath _positiveStatusContainerPath;
    [Export] private NodePath _negativeStatusContainerPath;
    [Export] private NodePath _limitBarPath;

    [ExportGroup("Charges")]
    [Export] private int _maxCharges = 9;
    [Export] private Vector2 _notchSize = new Vector2(10, 18);
    [Export] private int _notchSpacing = 2;
    [Export] private Texture2D _notchFilledTexture;
    [Export] private Texture2D _notchEmptyTexture;
    [Export] private Color _notchFilledColor = new Color(0.9f, 0.8f, 0.2f);
    [Export] private Color _notchEmptyColor = new Color(0.2f, 0.2f, 0.25f);
    [Export] private float _chargePulseScale = 1.2f;
    [Export] private float _chargePulseSeconds = 0.2f;
    [Export] private float _notchRotationDegrees = -12f;
    [Export] private bool _notchGlowEnabled = true;
    [Export] private float _notchGlowIntensity = 1.1f;
    [Export] private float _notchGlowAlpha = 0.9f;

    [ExportGroup("HP/MP Animation")]
    [Export] private float _hpTweenSeconds = 0.35f;
    [Export] private float _mpTweenSeconds = 0.35f;
    [Export] private Color _hpGainColor = new Color(0.4f, 1f, 0.6f);
    [Export] private Color _hpLossColor = new Color(1f, 0.4f, 0.4f);
    [Export] private Color _mpGainColor = new Color(0.5f, 0.8f, 1f);
    [Export] private Color _mpLossColor = new Color(0.7f, 0.4f, 1f);
    [Export] private string _hpPrefix = "HP";
    [Export] private string _mpPrefix = "MP";

    [ExportGroup("Bar Styling")]
    [Export] private bool _autoStyleBars = true;
    [Export] private Vector2 _barTextureSize = new Vector2(18, 7);
    [Export] private int _barOutlineThickness = 1;
    [Export] private Color _hpBarFill = new Color(0.35f, 0.9f, 0.55f, 0.95f);
    [Export] private Color _hpBarBack = new Color(0.1f, 0.15f, 0.12f, 0.9f);
    [Export] private Color _hpBarOutline = new Color(0.05f, 0.05f, 0.08f, 0.9f);
    [Export] private Color _mpBarFill = new Color(0.45f, 0.65f, 1f, 0.95f);
    [Export] private Color _mpBarBack = new Color(0.08f, 0.1f, 0.18f, 0.9f);
    [Export] private Color _mpBarOutline = new Color(0.05f, 0.05f, 0.08f, 0.9f);
    [Export] private Color _limitBarFill = new Color(1f, 0.55f, 0.25f, 0.95f);
    [Export] private Color _limitBarBack = new Color(0.15f, 0.08f, 0.05f, 0.9f);
    [Export] private Color _limitBarOutline = new Color(0.05f, 0.05f, 0.08f, 0.9f);

    [ExportGroup("Status Effects")]
    [Export] private Vector2 _statusIconSize = new Vector2(20, 20);
    [Export] private Color _positiveStatusTint = Colors.White;
    [Export] private Color _negativeStatusTint = new Color(1f, 0.6f, 0.6f);

    [ExportGroup("Active Highlight")]
    [Export] private Color _activeBorderColor = new Color(0.95f, 0.85f, 0.45f, 0.9f);
    [Export] private Color _activeGlowColor = new Color(1f, 0.9f, 0.5f, 0.6f);
    [Export] private float _activeGlowSize = 10f;
    [Export] private float _activePulseSeconds = 0.2f;

    [ExportGroup("Defeated State")]
    [Export] private Color _defeatedModulate = new Color(0.5f, 0.5f, 0.55f, 0.85f);
    [Export] private Color _defeatedTextColor = new Color(0.8f, 0.8f, 0.85f, 0.9f);

    private Label _nameLabel;
    private PanelContainer _panel;
    private TextureProgressBar _hpBar;
    private Label _hpNameLabel;
    private Label _hpCurrentLabel;
    private Label _hpMaxLabel;
    private TextureProgressBar _mpBar;
    private Label _mpNameLabel;
    private Label _mpCurrentLabel;
    private Label _mpMaxLabel;
    private HBoxContainer _chargeNotchContainer;
    private Control _positiveStatusContainer;
    private Control _negativeStatusContainer;
    private TextureProgressBar _limitBar;

    private readonly List<Control> _chargeNotches = new();
    private readonly Dictionary<string, TextureRect> _statusIcons = new();
    private Node _boundCharacter;
    private StatsComponent _stats;
    private StatusEffectManager _statusManager;
    private ChargeSystem _chargeSystem;

    private int _currentHp;
    private int _currentMp;
    private int _currentCharges;
    private bool _isActive;
    private bool _isDefeated;
    private StyleBoxFlat _normalPanelStyle;
    private StyleBoxFlat _activePanelStyle;
    private CanvasItemMaterial _notchGlowMaterial;
    private bool _labelStyleApplied;

    public override void _Ready()
    {
        CacheNodes();
        SetupChargeNotches();
        HookBarResizeSignals();
        CallDeferred(nameof(EnsureBarTextures));
    }

    public void Bind(Node character, ChargeSystem chargeSystem)
    {
        Unbind();

        _boundCharacter = character;
        _chargeSystem = chargeSystem;
        _stats = character?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        _statusManager = character?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);

        if (_nameLabel != null)
        {
            _nameLabel.Text = GetDisplayName(character);
        }

        if (_hpNameLabel != null)
        {
            _hpNameLabel.Text = _hpPrefix;
        }

        if (_mpNameLabel != null)
        {
            _mpNameLabel.Text = _mpPrefix;
        }

        EnsureLabelStyle();

        if (_stats != null)
        {
            _stats.CurrentHPChanged += OnHpChanged;
            _stats.CurrentMPChanged += OnMpChanged;
        }

        if (_statusManager != null)
        {
            _statusManager.StatusEffectApplied += OnStatusEffectChanged;
            _statusManager.StatusEffectRemoved += OnStatusEffectChanged;
            RefreshStatusIcons();
        }

        if (_chargeSystem != null)
        {
            _chargeSystem.ChargesChanged += OnChargesChanged;
            UpdateCharges(_chargeSystem.GetCharges(character), immediate: true);
        }

        if (_stats != null)
        {
            int maxHp = _stats.GetStatValue(StatType.HP);
            int maxMp = _stats.GetStatValue(StatType.MP);
            _currentHp = _stats.CurrentHP;
            _currentMp = _stats.CurrentMP;
            UpdateHp(_currentHp, maxHp, immediate: true);
            UpdateMp(_currentMp, maxMp, immediate: true);
            SetDefeated(_currentHp <= 0);
        }
    }

    public void Unbind()
    {
        if (_stats != null)
        {
            _stats.CurrentHPChanged -= OnHpChanged;
            _stats.CurrentMPChanged -= OnMpChanged;
        }

        if (_statusManager != null)
        {
            _statusManager.StatusEffectApplied -= OnStatusEffectChanged;
            _statusManager.StatusEffectRemoved -= OnStatusEffectChanged;
        }

        if (_chargeSystem != null)
        {
            _chargeSystem.ChargesChanged -= OnChargesChanged;
        }

        _stats = null;
        _statusManager = null;
        _chargeSystem = null;
        _boundCharacter = null;
    }

    private void CacheNodes()
    {
        _nameLabel = GetNodeOrNull<Label>(_nameLabelPath);
        _panel = GetNodeOrNull<PanelContainer>(_panelPath);
        _hpBar = GetNodeOrNull<TextureProgressBar>(_hpBarPath);
        _hpNameLabel = GetNodeOrNull<Label>(_hpNameLabelPath);
        _hpCurrentLabel = GetNodeOrNull<Label>(_hpCurrentLabelPath);
        _hpMaxLabel = GetNodeOrNull<Label>(_hpMaxLabelPath);
        _mpBar = GetNodeOrNull<TextureProgressBar>(_mpBarPath);
        _mpNameLabel = GetNodeOrNull<Label>(_mpNameLabelPath);
        _mpCurrentLabel = GetNodeOrNull<Label>(_mpCurrentLabelPath);
        _mpMaxLabel = GetNodeOrNull<Label>(_mpMaxLabelPath);
        _chargeNotchContainer = GetNodeOrNull<HBoxContainer>(_chargeNotchContainerPath);
        _positiveStatusContainer = GetNodeOrNull<Control>(_positiveStatusContainerPath);
        _negativeStatusContainer = GetNodeOrNull<Control>(_negativeStatusContainerPath);
        _limitBar = GetNodeOrNull<TextureProgressBar>(_limitBarPath);
    }

    public void SetActive(bool isActive)
    {
        if (_isActive == isActive) return;
        _isActive = isActive;

        EnsurePanelStyles();
        if (_panel == null) return;

        if (_isDefeated)
        {
            _panel.AddThemeStyleboxOverride("panel", _normalPanelStyle);
            ApplyDefeatedVisuals();
            return;
        }

        _panel.AddThemeStyleboxOverride("panel", isActive ? _activePanelStyle : _normalPanelStyle);

        var tween = CreateTween();
        _panel.Modulate = Colors.White;
        if (isActive)
        {
            tween.TweenProperty(_panel, "modulate", new Color(1f, 1f, 1f, 1f), _activePulseSeconds)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }
        else
        {
            tween.TweenProperty(_panel, "modulate", Colors.White, _activePulseSeconds)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }
    }

    public void SetDefeated(bool isDefeated)
    {
        if (_isDefeated == isDefeated) return;
        _isDefeated = isDefeated;
        ApplyDefeatedVisuals();
    }

    private void ApplyDefeatedVisuals()
    {
        if (_panel == null) return;

        if (_isDefeated)
        {
            _panel.AddThemeStyleboxOverride("panel", _normalPanelStyle ?? _panel.GetThemeStylebox("panel"));
            _panel.Modulate = _defeatedModulate;
            if (_nameLabel != null) _nameLabel.Modulate = _defeatedTextColor;
        }
        else
        {
            _panel.Modulate = Colors.White;
            if (_nameLabel != null) _nameLabel.Modulate = Colors.White;
            _panel.AddThemeStyleboxOverride("panel", _isActive ? _activePanelStyle : _normalPanelStyle);
        }
    }

    private void EnsurePanelStyles()
    {
        if (_panel == null) return;
        if (_normalPanelStyle != null && _activePanelStyle != null) return;

        var baseStyle = _panel.GetThemeStylebox("panel") as StyleBoxFlat;
        _normalPanelStyle = baseStyle != null ? (StyleBoxFlat)baseStyle.Duplicate() : new StyleBoxFlat();
        _activePanelStyle = (StyleBoxFlat)_normalPanelStyle.Duplicate();

        _activePanelStyle.BorderColor = _activeBorderColor;
        _activePanelStyle.ShadowColor = _activeGlowColor;
        _activePanelStyle.ShadowSize = (int)Mathf.Round(_activeGlowSize);
    }

    private void EnsureBarTextures()
    {
        if (!_autoStyleBars) return;

        ApplyBarStyle(_hpBar, _hpBarFill, _hpBarBack, _hpBarOutline);
        ApplyBarStyle(_mpBar, _mpBarFill, _mpBarBack, _mpBarOutline);
        ApplyBarStyle(_limitBar, _limitBarFill, _limitBarBack, _limitBarOutline);
    }

    private void ApplyBarStyle(TextureProgressBar bar, Color fill, Color back, Color outline)
    {
        if (bar == null) return;

        var size = GetBarTextureSize(bar);

        bool needsUpdate = bar.TextureProgress == null || bar.TextureUnder == null || bar.TextureOver == null;
        if (!needsUpdate && bar.TextureProgress != null)
        {
            var currentSize = bar.TextureProgress.GetSize();
            if (Mathf.Abs(currentSize.X - size.X) > 2 || Mathf.Abs(currentSize.Y - size.Y) > 2)
            {
                needsUpdate = true;
            }
        }

        if (!needsUpdate) return;

        bar.TextureUnder = CreateSolidTexture(size, back);
        bar.TextureProgress = CreateSolidTexture(size, fill);
        bar.TextureOver = CreateOutlineTexture(size, outline, _barOutlineThickness);
    }

    private Vector2I GetBarTextureSize(TextureProgressBar bar)
    {
        float width = Mathf.Max(bar.Size.X, Mathf.Max(bar.CustomMinimumSize.X, _barTextureSize.X));
        float height = Mathf.Max(bar.Size.Y, Mathf.Max( bar.CustomMinimumSize.Y, _barTextureSize.Y));
        int w = Mathf.Max(4, Mathf.RoundToInt(width));
        int h = Mathf.Max(2, Mathf.RoundToInt(height));
        return new Vector2I(w, h);
    }

    private void HookBarResizeSignals()
    {
        if (_hpBar != null) _hpBar.Resized += OnBarResized;
        if (_mpBar != null) _mpBar.Resized += OnBarResized;
        if (_limitBar != null) _limitBar.Resized += OnBarResized;
    }

    private void OnBarResized()
    {
        EnsureBarTextures();
    }

    private static Texture2D CreateSolidTexture(Vector2I size, Color color)
    {
        var image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        image.Fill(color);
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateOutlineTexture(Vector2I size, Color outline, int thickness)
    {
        var image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        image.Fill(new Color(0, 0, 0, 0));
        int t = Mathf.Max(1, thickness);

        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                bool isBorder = x < t || x >= size.X - t || y < t || y >= size.Y - t;
                if (isBorder)
                {
                    image.SetPixel(x, y, outline);
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private void SetupChargeNotches()
    {
        if (_chargeNotchContainer == null) return;

        foreach (var child in _chargeNotchContainer.GetChildren())
        {
            child.QueueFree();
        }
        _chargeNotches.Clear();

        _chargeNotchContainer.AddThemeConstantOverride("separation", _notchSpacing);
        _chargeNotchContainer.ClipContents = false;

        for (int i = 0; i < _maxCharges; i++)
        {
            Control notch;
            if (_notchFilledTexture != null || _notchEmptyTexture != null)
            {
                var rect = new TextureRect();
                rect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                rect.CustomMinimumSize = _notchSize;
                rect.Size = _notchSize;
                rect.ClipContents = false;
                ConfigureNotchTransform(rect);
                notch = rect;
            }
            else
            {
                var rect = new ColorRect();
                rect.CustomMinimumSize = _notchSize;
                rect.Size = _notchSize;
                rect.ClipContents = false;
                ConfigureNotchTransform(rect);
                notch = rect;
            }

            _chargeNotchContainer.AddChild(notch);
            _chargeNotches.Add(notch);
        }

        CallDeferred(nameof(ApplyNotchTransforms));
        UpdateCharges(0, immediate: true);
    }

    private void UpdateCharges(int charges, bool immediate = false, int delta = 0)
    {
        _currentCharges = Mathf.Clamp(charges, 0, _maxCharges);

        for (int i = 0; i < _chargeNotches.Count; i++)
        {
            bool filled = i < _currentCharges;
            var notch = _chargeNotches[i];
            ConfigureNotchTransform(notch);

            ApplyNotchVisual(notch, filled);
        }

        if (!immediate && delta != 0 && _currentCharges > 0 && _currentCharges <= _chargeNotches.Count)
        {
            int index = Mathf.Clamp(_currentCharges - 1, 0, _chargeNotches.Count - 1);
            if (delta < 0)
            {
                index = Mathf.Clamp(_currentCharges, 0, _chargeNotches.Count - 1);
            }

            var notch = _chargeNotches[index];
            var tween = CreateTween();
            notch.PivotOffset = notch.Size / 2.0f;
            tween.TweenProperty(notch, "scale", Vector2.One * _chargePulseScale, _chargePulseSeconds * 0.5f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(notch, "scale", Vector2.One, _chargePulseSeconds * 0.5f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
        }
    }

    private void UpdateHp(int value, int maxValue, bool immediate = false)
    {
        if (_hpBar != null)
        {
            _hpBar.MaxValue = maxValue;
            if (immediate)
            {
                _hpBar.Value = value;
            }
            else
            {
                var tween = CreateTween();
                tween.TweenProperty(_hpBar, "value", value, _hpTweenSeconds)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            }
        }

        UpdateValueLabels(_hpCurrentLabel, _hpMaxLabel, _currentHp, value, maxValue, immediate, value >= _currentHp ? _hpGainColor : _hpLossColor, _hpTweenSeconds, _hpPrefix);
        _currentHp = value;
        SetDefeated(_currentHp <= 0);
    }

    private void UpdateMp(int value, int maxValue, bool immediate = false)
    {
        if (_mpBar != null)
        {
            _mpBar.MaxValue = maxValue;
            if (immediate)
            {
                _mpBar.Value = value;
            }
            else
            {
                var tween = CreateTween();
                tween.TweenProperty(_mpBar, "value", value, _mpTweenSeconds)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            }
        }

        UpdateValueLabels(_mpCurrentLabel, _mpMaxLabel, _currentMp, value, maxValue, immediate, value >= _currentMp ? _mpGainColor : _mpLossColor, _mpTweenSeconds, _mpPrefix);
        _currentMp = value;
    }

    private void UpdateValueLabels(Label currentLabel, Label maxLabel, int startValue, int value, int maxValue, bool immediate, Color pulseColor, float duration, string prefix)
    {
        if (maxLabel != null)
        {
            maxLabel.Text = $"/{maxValue}";
        }

        if (currentLabel == null) return;

        if (immediate)
        {
            currentLabel.Text = $"{value}";
            return;
        }

        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v =>
        {
            int displayValue = Mathf.RoundToInt(v);
            currentLabel.Text = $"{displayValue}";
        }), startValue, value, duration).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        tween.Parallel().TweenProperty(currentLabel, "modulate", pulseColor, duration * 0.5f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(currentLabel, "modulate", Colors.White, duration * 0.5f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    }

    private void ApplyNotchVisual(Control notch, bool filled)
    {
        var glowMaterial = GetNotchGlowMaterial();

        if (notch is TextureRect rect)
        {
            rect.Texture = filled ? _notchFilledTexture : _notchEmptyTexture;
            rect.Modulate = filled ? AdjustGlowColor(_notchFilledColor, true) : _notchEmptyColor;
            rect.Material = filled ? glowMaterial : null;
        }
        else if (notch is ColorRect colorRect)
        {
            colorRect.Color = filled ? AdjustGlowColor(_notchFilledColor, true) : _notchEmptyColor;
            colorRect.Material = filled ? glowMaterial : null;
        }
    }

    private Color AdjustGlowColor(Color color, bool filled)
    {
        if (!filled || !_notchGlowEnabled) return color;
        return new Color(
            Mathf.Clamp(color.R * _notchGlowIntensity, 0f, 1f),
            Mathf.Clamp(color.G * _notchGlowIntensity, 0f, 1f),
            Mathf.Clamp(color.B * _notchGlowIntensity, 0f, 1f),
            Mathf.Clamp(color.A * _notchGlowAlpha, 0f, 1f)
        );
    }

    private CanvasItemMaterial GetNotchGlowMaterial()
    {
        if (!_notchGlowEnabled) return null;
        if (_notchGlowMaterial != null) return _notchGlowMaterial;

        _notchGlowMaterial = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded
        };
        return _notchGlowMaterial;
    }

    private void ConfigureNotchTransform(Control notch)
    {
        if (notch == null) return;
        notch.CustomMinimumSize = _notchSize;
        var size = notch.Size;
        if (size == Vector2.Zero)
        {
            size = _notchSize;
            notch.Size = size;
        }

        var pivot = size / 2.0f;
        var rotation = Mathf.DegToRad(_notchRotationDegrees);
        notch.PivotOffset = pivot;
        notch.Rotation = rotation;
        notch.SetDeferred("pivot_offset", pivot);
        notch.SetDeferred("rotation", rotation);
    }

    private void ApplyNotchTransforms()
    {
        foreach (var notch in _chargeNotches)
        {
            ConfigureNotchTransform(notch);
        }
    }

    private void EnsureLabelStyle()
    {
        if (_labelStyleApplied) return;
        ApplyLabelStyle(_nameLabel, 3, new Color(0, 0, 0, 0.8f));
        ApplyLabelStyle(_hpNameLabel, 2, new Color(0, 0, 0, 0.7f));
        ApplyLabelStyle(_hpCurrentLabel, 2, new Color(0, 0, 0, 0.7f));
        ApplyLabelStyle(_hpMaxLabel, 2, new Color(0, 0, 0, 0.7f));
        ApplyLabelStyle(_mpNameLabel, 2, new Color(0, 0, 0, 0.7f));
        ApplyLabelStyle(_mpCurrentLabel, 2, new Color(0, 0, 0, 0.7f));
        ApplyLabelStyle(_mpMaxLabel, 2, new Color(0, 0, 0, 0.7f));
        _labelStyleApplied = true;
    }

    private static void ApplyLabelStyle(Label label, int outlineSize, Color outlineColor)
    {
        if (label == null) return;
        var existing = label.LabelSettings;
        var settings = existing != null
            ? (LabelSettings)existing.Duplicate()
            : new LabelSettings();

        settings.Font = label.GetThemeFont("font");
        int themeSize = label.GetThemeFontSize("font_size");
        if (themeSize > 0)
        {
            settings.FontSize = themeSize;
        }

        settings.OutlineSize = outlineSize;
        settings.OutlineColor = outlineColor;
        settings.FontColor = Colors.White;
        label.LabelSettings = settings;
    }

    private void RefreshStatusIcons()
    {
        if (_statusManager == null) return;

        var desiredKeys = new HashSet<string>();
        foreach (var instance in _statusManager.GetActiveEffects())
        {
            if (instance?.EffectData == null) continue;
            var effect = instance.EffectData;
            var icon = effect.Icon as Texture2D;
            if (icon == null) continue;

            var key = GetEffectKey(effect);
            if (string.IsNullOrEmpty(key)) continue;
            desiredKeys.Add(key);

            if (_statusIcons.TryGetValue(key, out var existing))
            {
                existing.Texture = icon;
                existing.Modulate = effect.Polarity == StatusEffectPolarity.Negative ? _negativeStatusTint : _positiveStatusTint;
                existing.CustomMinimumSize = _statusIconSize;
                existing.Size = _statusIconSize;
                continue;
            }

            var rect = new TextureRect
            {
                Texture = icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = _statusIconSize,
                Modulate = effect.Polarity == StatusEffectPolarity.Negative ? _negativeStatusTint : _positiveStatusTint
            };
            rect.Size = _statusIconSize;

            rect.Scale = new Vector2(0.4f, 0.4f);
            rect.Modulate = new Color(rect.Modulate.R, rect.Modulate.G, rect.Modulate.B, 0f);

            if (effect.Polarity == StatusEffectPolarity.Negative)
            {
                _negativeStatusContainer?.AddChild(rect);
            }
            else
            {
                _positiveStatusContainer?.AddChild(rect);
            }

            _statusIcons[key] = rect;

            var tween = CreateTween();
            tween.TweenProperty(rect, "scale", Vector2.One, 0.18f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.Parallel().TweenProperty(rect, "modulate:a", 1.0f, 0.18f);
        }

        var toRemove = _statusIcons.Keys.Where(k => !desiredKeys.Contains(k)).ToList();
        foreach (var key in toRemove)
        {
            if (!_statusIcons.TryGetValue(key, out var rect)) continue;
            _statusIcons.Remove(key);

            if (!IsInstanceValid(rect)) continue;
            var tween = CreateTween();
            tween.TweenProperty(rect, "scale", Vector2.Zero, 0.12f)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tween.Parallel().TweenProperty(rect, "modulate:a", 0.0f, 0.12f);
            tween.TweenCallback(Callable.From(rect.QueueFree));
        }
    }

    private void ClearStatusContainer(Control container)
    {
        if (container == null) return;
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnHpChanged(int newHp, int maxHp)
    {
        UpdateHp(newHp, maxHp);
    }

    private void OnMpChanged(int newMp, int maxMp)
    {
        UpdateMp(newMp, maxMp);
    }

    private void OnStatusEffectChanged(StatusEffect effect, Node owner)
    {
        if (owner != _boundCharacter) return;
        RefreshStatusIcons();
    }

    private void OnChargesChanged(Node character, int newValue, int delta)
    {
        if (character != _boundCharacter) return;
        UpdateCharges(newValue, immediate: false, delta: delta);
    }

    private static string GetDisplayName(Node member)
    {
        if (member is BaseCharacter baseCharacter)
        {
            var displayName = baseCharacter.PresentationData?.DisplayName;
            if (!string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }
        }
        return member?.Name ?? "Unknown";
    }

    private static string GetEffectKey(StatusEffect effect)
    {
        if (effect == null) return null;
        if (!string.IsNullOrEmpty(effect.ResourcePath)) return effect.ResourcePath;
        if (!string.IsNullOrEmpty(effect.EffectName)) return effect.EffectName;
        if (!string.IsNullOrEmpty(effect.ResourceName)) return effect.ResourceName;
        return null;
    }
}
