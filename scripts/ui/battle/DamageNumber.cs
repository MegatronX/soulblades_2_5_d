using Godot;
using System.Collections.Generic;

public partial class DamageNumber : Control
{
    [Export] private Label _label;
    [Export] private Control _iconsContainer;
    [Export] private Godot.Collections.Dictionary<ElementType, Texture2D> _elementIcons;

    [ExportGroup("Style/Base")]
    [Export] private int _baseFontSize = 24;
    [Export] private Color _baseFontColor = Colors.White;
    [Export] private int _outlineSize = 4;
    [Export] private Color _outlineColor = Colors.Black;

    [ExportGroup("Style/Heal")]
    [Export] private Color _healFontColor = Colors.LightGreen;
    [Export] private int _healFontSize = 26;
    [Export] private string _healPrefix = "+";

    [ExportGroup("Style/Crit")]
    [Export] private Color _critFontColor = new Color(1f, 0.9f, 0.2f);
    [Export] private int _critFontSize = 32;
    [Export] private float _critPunchScale = 1.2f;

    [ExportGroup("Style/Timed Hit")]
    [Export] private Color _greatFontColor = Colors.Orange;
    [Export] private Color _perfectFontColor = Colors.Gold;
    [Export] private int _perfectFontSize = 36;
    [Export] private string _perfectSuffix = "!!";

    [ExportGroup("Style/Miss")]
    [Export] private string _missText = "Miss";
    [Export] private Color _missFontColor = new Color(0.8f, 0.85f, 1.0f);
    [Export] private int _missFontSize = 24;
    [Export] private bool _missDisableGlow = true;

    [ExportGroup("Style/Glow")]
    [Export] private bool _enableGlow = true;
    [Export] private Color _glowColor = new Color(0.35f, 0.8f, 1.0f, 0.75f);
    [Export] private int _glowSize = 6;
    [Export] private Vector2 _glowOffset = Vector2.Zero;
    [Export] private bool _useHealGlow = true;
    [Export] private Color _healGlowColor = new Color(0.4f, 1.0f, 0.6f, 0.8f);
    [Export] private bool _useCritGlow = true;
    [Export] private Color _critGlowColor = new Color(1.0f, 0.85f, 0.2f, 0.9f);
    [Export] private int _critGlowSize = 8;
    [Export] private Color _perfectGlowColor = new Color(1.0f, 0.95f, 0.4f, 1.0f);
    [Export] private int _perfectGlowSize = 10;

    [ExportGroup("Animation/Pop")]
    [Export] private float _popDuration = 0.2f;
    [Export] private float _popOvershootScale = 1.05f;
    [Export] private float _popOvershootDuration = 0.08f;

    [ExportGroup("Animation/Float")]
    [Export] private float _floatDistance = 50f;
    [Export] private float _floatDuration = 1.0f;

    [ExportGroup("Animation/Fade")]
    [Export] private float _fadeDelay = 0.7f;
    [Export] private float _fadeDuration = 0.3f;

    [ExportGroup("Animation/Impact")]
    [Export] private float _impactShakePixels = 6f;
    [Export] private float _impactShakeDuration = 0.2f;
    [Export] private float _impactRotateDegrees = 6f;
    
    private Node3D _target;
    private bool _hasFallbackWorldPosition;
    private Vector3 _fallbackWorldPosition;
    private Vector3 _worldOffset = new Vector3(0, 1.5f, 0);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        if (_label != null) _label.MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Configure(Node target, int amount, bool isCrit, bool isHeal, TimedHitRating rating, Dictionary<ElementType, float> elements = null, Vector3? fallbackWorldPosition = null)
    {
        _target = target as Node3D;
        if (fallbackWorldPosition.HasValue)
        {
            _hasFallbackWorldPosition = true;
            _fallbackWorldPosition = fallbackWorldPosition.Value;
        }
        else
        {
            _hasFallbackWorldPosition = false;
        }
        if (_label == null) _label = GetNodeOrNull<Label>("Label");
        if (_label == null) return;

        _label.Text = Mathf.Abs(amount).ToString();
        _label.PivotOffset = _label.Size / 2;

        // Setup Icons
        if (_iconsContainer != null)
        {
            // Clear previous icons (if pooled)
            foreach (Node child in _iconsContainer.GetChildren()) child.QueueFree();

            if (elements != null && _elementIcons != null)
            {
                foreach (var kvp in elements)
                {
                    // Only show icon if damage contribution is significant (> 0) and we have an icon
                    if (kvp.Value > 0 && _elementIcons.TryGetValue(kvp.Key, out Texture2D icon))
                    {
                        var rect = new TextureRect();
                        rect.Texture = icon;
                        rect.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
                        rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                        rect.CustomMinimumSize = new Vector2(24, 24);
                        _iconsContainer.AddChild(rect);
                    }
                }
            }
        }

        // Base Style
        var settings = _label.LabelSettings != null ? (LabelSettings)_label.LabelSettings.Duplicate() : new LabelSettings();
        settings.OutlineSize = _outlineSize;
        settings.OutlineColor = _outlineColor;
        settings.FontSize = _baseFontSize;
        settings.FontColor = _baseFontColor;
        
        if (isHeal)
        {
            settings.FontColor = _healFontColor;
            if (_healFontSize > 0) settings.FontSize = _healFontSize;
            _label.Text = _healPrefix + _label.Text;
        }
        else if (isCrit)
        {
            settings.FontColor = _critFontColor;
            if (_critFontSize > 0) settings.FontSize = _critFontSize;
        }

        // Timed Hit Flair
        if (!isHeal)
        {
            if (rating == TimedHitRating.Perfect)
            {
                settings.FontColor = _perfectFontColor;
                if (_perfectFontSize > 0) settings.FontSize = _perfectFontSize;
                if (!string.IsNullOrEmpty(_perfectSuffix)) _label.Text += _perfectSuffix;
            }
            else if (rating == TimedHitRating.Great)
            {
                settings.FontColor = _greatFontColor;
            }
        }

        if (_enableGlow)
        {
            var glowColor = _glowColor;
            var glowSize = _glowSize;

            if (rating == TimedHitRating.Perfect)
            {
                glowColor = _perfectGlowColor;
                glowSize = _perfectGlowSize;
            }
            else if (isCrit && _useCritGlow)
            {
                glowColor = _critGlowColor;
                glowSize = _critGlowSize;
            }
            else if (isHeal && _useHealGlow)
            {
                glowColor = _healGlowColor;
            }

            settings.ShadowColor = glowColor;
            settings.ShadowOffset = _glowOffset;
            settings.ShadowSize = glowSize;
        }

        _label.LabelSettings = settings;

        Animate(isCrit, rating);
    }

    public void ConfigureMiss(Node target, Vector3? fallbackWorldPosition = null)
    {
        _target = target as Node3D;
        if (fallbackWorldPosition.HasValue)
        {
            _hasFallbackWorldPosition = true;
            _fallbackWorldPosition = fallbackWorldPosition.Value;
        }
        else
        {
            _hasFallbackWorldPosition = false;
        }
        if (_label == null) _label = GetNodeOrNull<Label>("Label");
        if (_label == null) return;

        _label.Text = _missText;
        _label.PivotOffset = _label.Size / 2;

        if (_iconsContainer != null)
        {
            foreach (Node child in _iconsContainer.GetChildren()) child.QueueFree();
        }

        var settings = _label.LabelSettings != null ? (LabelSettings)_label.LabelSettings.Duplicate() : new LabelSettings();
        settings.OutlineSize = _outlineSize;
        settings.OutlineColor = _outlineColor;
        settings.FontSize = _missFontSize > 0 ? _missFontSize : _baseFontSize;
        settings.FontColor = _missFontColor;

        if (_enableGlow && !_missDisableGlow)
        {
            settings.ShadowColor = _glowColor;
            settings.ShadowOffset = _glowOffset;
            settings.ShadowSize = _glowSize;
        }
        else
        {
            settings.ShadowSize = 0;
        }

        _label.LabelSettings = settings;

        Animate(isCrit: false, rating: TimedHitRating.Miss);
    }

    private void Animate(bool isCrit, TimedHitRating rating)
    {
        var tween = CreateTween();
        
        // Pop
        _label.Scale = Vector2.Zero;
        tween.TweenProperty(_label, "scale", Vector2.One, _popDuration).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        if (_popOvershootScale > 1f)
        {
            tween.TweenProperty(_label, "scale", Vector2.One * _popOvershootScale, _popOvershootDuration)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(_label, "scale", Vector2.One, _popOvershootDuration)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        }
        
        if (isCrit || rating == TimedHitRating.Perfect)
        {
            if (_critPunchScale > 1f)
            {
                tween.TweenProperty(_label, "scale", Vector2.One * _critPunchScale, 0.1f);
                tween.TweenProperty(_label, "scale", Vector2.One, 0.1f);
            }

            if (_impactShakePixels > 0f)
            {
                var basePos = _label.Position;
                tween.Parallel().TweenProperty(_label, "position:x", basePos.X + _impactShakePixels, _impactShakeDuration * 0.5f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                tween.Parallel().TweenProperty(_label, "position:x", basePos.X - _impactShakePixels, _impactShakeDuration * 0.5f)
                    .SetDelay(_impactShakeDuration * 0.5f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
                tween.Parallel().TweenProperty(_label, "position:x", basePos.X, _impactShakeDuration * 0.25f)
                    .SetDelay(_impactShakeDuration);
            }

            if (_impactRotateDegrees > 0f)
            {
                _label.RotationDegrees = -_impactRotateDegrees;
                tween.Parallel().TweenProperty(_label, "rotation_degrees", _impactRotateDegrees, _impactShakeDuration * 0.5f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                tween.Parallel().TweenProperty(_label, "rotation_degrees", 0f, _impactShakeDuration * 0.5f)
                    .SetDelay(_impactShakeDuration * 0.5f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            }
        }

        // Float
        Vector2 startPos = _label.Position;
        tween.Parallel().TweenProperty(_label, "position:y", startPos.Y - _floatDistance, _floatDuration)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        
        // Fade
        tween.Parallel().TweenProperty(_label, "modulate:a", 0.0f, _fadeDuration).SetDelay(_fadeDelay);
        
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    public override void _Process(double delta)
    {
        if (IsInstanceValid(_target))
        {
            var cam = GetViewport().GetCamera3D();
            if (cam != null && !cam.IsPositionBehind(_target.GlobalPosition))
            {
                Vector2 screenPos = cam.UnprojectPosition(_target.GlobalPosition + _worldOffset);
                GlobalPosition = screenPos - (Size / 2);
            }
            else
            {
                Hide();
            }
        }
        else if (_hasFallbackWorldPosition)
        {
            var cam = GetViewport().GetCamera3D();
            if (cam != null && !cam.IsPositionBehind(_fallbackWorldPosition))
            {
                Vector2 screenPos = cam.UnprojectPosition(_fallbackWorldPosition + _worldOffset);
                GlobalPosition = screenPos - (Size / 2);
            }
            else
            {
                Hide();
            }
        }
        else
        {
            QueueFree();
        }
    }
}
