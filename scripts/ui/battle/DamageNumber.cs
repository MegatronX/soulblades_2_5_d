using Godot;
using System.Collections.Generic;

public partial class DamageNumber : Control
{
    [Export] private Label _label;
    [Export] private Control _iconsContainer;
    [Export] private Godot.Collections.Dictionary<ElementType, Texture2D> _elementIcons;
    
    private Node3D _target;
    private Vector3 _worldOffset = new Vector3(0, 1.5f, 0);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        if (_label != null) _label.MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Configure(Node target, int amount, bool isCrit, bool isHeal, TimedHitRating rating, Dictionary<ElementType, float> elements = null)
    {
        _target = target as Node3D;
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
        settings.OutlineSize = 4;
        settings.OutlineColor = Colors.Black;
        settings.FontSize = 24;
        settings.FontColor = Colors.White;
        
        if (isHeal)
        {
            settings.FontColor = Colors.LightGreen;
            _label.Text = "+" + _label.Text;
        }
        else if (isCrit)
        {
            settings.FontColor = new Color(1f, 0.9f, 0.2f); // Yellowish
            settings.FontSize = 32;
        }

        // Timed Hit Flair
        if (!isHeal)
        {
            if (rating == TimedHitRating.Perfect)
            {
                settings.FontColor = Colors.Gold;
                settings.FontSize = 36;
                _label.Text += "!!";
            }
            else if (rating == TimedHitRating.Great)
            {
                settings.FontColor = Colors.Orange;
            }
        }

        _label.LabelSettings = settings;

        Animate(isCrit, rating);
    }

    private void Animate(bool isCrit, TimedHitRating rating)
    {
        var tween = CreateTween();
        
        // Pop
        _label.Scale = Vector2.Zero;
        tween.TweenProperty(_label, "scale", Vector2.One, 0.2f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        
        if (isCrit || rating == TimedHitRating.Perfect)
        {
            tween.TweenProperty(_label, "scale", Vector2.One * 1.2f, 0.1f);
            tween.TweenProperty(_label, "scale", Vector2.One, 0.1f);
        }

        // Float
        Vector2 startPos = _label.Position;
        tween.Parallel().TweenProperty(_label, "position:y", startPos.Y - 50, 1.0f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        
        // Fade
        tween.Parallel().TweenProperty(_label, "modulate:a", 0.0f, 0.3f).SetDelay(0.7f);
        
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
        else
        {
            QueueFree();
        }
    }
}