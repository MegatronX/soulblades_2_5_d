using Godot;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

/// <summary>
/// A UI component that displays a list of commands and supports a "folding out" animation.
/// </summary>
public partial class FoldableMenu : PanelContainer
{
    [Signal]
    public delegate void CommandSelectedEventHandler(BattleCommand command, int index);

    [Export] private VBoxContainer _listContainer;
    [Export] private TextureRect _backgroundRect;
    
    private List<Button> _buttons = new();
    private MenuTheme _currentTheme;
    private Label _sidebarLabel;
    private Control _sidebarWrapper;
    private float _expandedWidth;
    private bool _suppressFocusSound = false;

    public override void _Ready()
    {
        // Ensure pivot is set for folding animation (e.g., Top-Left or Center-Left)
        PivotOffset = new Vector2(0, Size.Y / 2);

        // Create a wrapper Control to hold the label.
        // This isolates the label from the PanelContainer's direct layout constraints.
        _sidebarWrapper = new Control();
        _sidebarWrapper.Name = "SidebarWrapper";
        _sidebarWrapper.MouseFilter = MouseFilterEnum.Ignore;
        _sidebarWrapper.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _sidebarWrapper.SizeFlagsVertical = SizeFlags.ExpandFill;
        _sidebarWrapper.Hide();
        AddChild(_sidebarWrapper);

        _sidebarLabel = new Label();
        _sidebarLabel.RotationDegrees = -90;
        _sidebarLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _sidebarLabel.VerticalAlignment = VerticalAlignment.Center;
        _sidebarWrapper.AddChild(_sidebarLabel);
    }

    public void BuildMenu(List<BattleCommand> commands, MenuTheme theme)
    {
        // Reset constraints so the menu can calculate its natural size based on new buttons
        CustomMinimumSize = Vector2.Zero;

        _currentTheme = theme;
        ApplyTheme();

        foreach (Node child in _listContainer.GetChildren())
        {
            child.QueueFree();
        }
        _buttons.Clear();

        // Create new buttons
        for (int i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            var btn = new Button();
            btn.Text = cmd.CommandName;
            Texture2D icon = cmd.Icon;
            if (icon == null && theme != null)
            {
                if (theme.CommandIcons.TryGetValue(cmd.CommandName, out var specificIcon))
                {
                    icon = specificIcon;
                }
                else
                {
                    icon = theme.DefaultIcon;
                }
            }
            btn.Icon = icon;
            btn.Alignment = HorizontalAlignment.Center;
            btn.ExpandIcon = true;
            
            // Styling
            btn.Flat = true;
            if (theme != null)
            {
                btn.AddThemeColorOverride("font_color", theme.TextColor);
                btn.AddThemeColorOverride("font_hover_color", theme.SelectedTextColor);
                btn.AddThemeColorOverride("font_focus_color", theme.SelectedTextColor);

                if (theme.ButtonBackground != null)
                {
                    var styleBox = new StyleBoxTexture { Texture = theme.ButtonBackground };
                    btn.AddThemeStyleboxOverride("normal", styleBox);
                    btn.AddThemeStyleboxOverride("hover", styleBox);
                    btn.AddThemeStyleboxOverride("pressed", styleBox);
                    btn.AddThemeStyleboxOverride("focus", styleBox);
                    btn.Flat = false;
                }

                if (theme.ButtonHeight > 0)
                {
                    btn.CustomMinimumSize = new Vector2(0, theme.ButtonHeight);
                }

                if (theme.FontSize > 0)
                {
                    btn.AddThemeFontSizeOverride("font_size", theme.FontSize);
                }
            }

            int index = i; // Capture for closure
            btn.Pressed += () => EmitSignal(SignalName.CommandSelected, cmd, index);
            
            // Wrap in MarginContainer for sliding animation
            var wrapper = new MarginContainer();
            wrapper.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            wrapper.AddChild(btn);
            
            // Animation & Focus logic
            btn.FocusEntered += () => 
            {
                AnimateButtonSlide(wrapper, true);
                if (!_suppressFocusSound) UISoundManager.Instance?.Play(UISoundType.Navigation);
            };
            btn.FocusExited += () => AnimateButtonSlide(wrapper, false);
            btn.MouseEntered += btn.GrabFocus; // Unify mouse hover and keyboard focus

            // Sound logic for confirmation
            btn.Pressed += () => UISoundManager.Instance?.Play(UISoundType.Confirm);

            _listContainer.AddChild(wrapper);
            _buttons.Add(btn);
        }

        // Setup Focus Neighbors for Wrap-Around
        if (_buttons.Count > 1)
        {
            var firstBtn = _buttons[0];
            var lastBtn = _buttons[_buttons.Count - 1];

            firstBtn.FocusNeighborTop = lastBtn.GetPath();
            lastBtn.FocusNeighborBottom = firstBtn.GetPath();
        }

        // Defer the animation start to allow the Container to resize based on new children
        CallDeferred(nameof(StartOpenAnimation));
    }

    private void AnimateButtonSlide(MarginContainer wrapper, bool slideRight)
    {
        int startLeft = wrapper.GetThemeConstant("margin_left");
        int targetLeft = slideRight ? 20 : 0; // Slide 20px to the right
        
        int startRight = wrapper.GetThemeConstant("margin_right");
        int targetRight = slideRight ? -20 : 0; // Negative margin extends the boundary to preserve width
        
        if (startLeft == targetLeft) return;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenMethod(Callable.From<int>(v => wrapper.AddThemeConstantOverride("margin_left", v)), startLeft, targetLeft, 0.1f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenMethod(Callable.From<int>(v => wrapper.AddThemeConstantOverride("margin_right", v)), startRight, targetRight, 0.1f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    private void StartOpenAnimation()
    {
        PivotOffset = new Vector2(0, Size.Y / 2); // Center-Left pivot
        Scale = new Vector2(1, 0);
        Show();
        AnimateOpen();
    }

    public async Task HideMenu()
    {
        float duration = _currentTheme?.CloseAnimationDuration ?? 0.15f;
        // Animate closing
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1, 0), duration)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        
        await ToSignal(tween, Tween.SignalName.Finished);
        Hide();
    }

    private void AnimateOpen()
    {
        float duration = _currentTheme?.OpenAnimationDuration ?? 0.25f;
        // "Fold out" animation
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1, 1), duration)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private void ApplyTheme()
    {
        if (_currentTheme == null) return;
        
        if (_backgroundRect != null)
        {
            _backgroundRect.Texture = _currentTheme.BackgroundTexture;
        }
        
        if (_listContainer != null)
        {
            _listContainer.AddThemeConstantOverride("separation", _currentTheme.ButtonSpacing);
        }

        if (_sidebarLabel != null)
        {
            _sidebarLabel.AddThemeColorOverride("font_color", _currentTheme.TextColor);
            if (_currentTheme.FontSize > 0)
            {
                _sidebarLabel.AddThemeFontSizeOverride("font_size", _currentTheme.FontSize);
            }
        }
    }

    public void FocusFirst()
    {
        _suppressFocusSound = true;
        if (_buttons.Count > 0) _buttons[0].GrabFocus();
        _suppressFocusSound = false;
    }

    public void FocusIndex(int index)
    {
        _suppressFocusSound = true;
        if (index >= 0 && index < _buttons.Count)
        {
            _buttons[index].GrabFocus();
        }
        else
        {
            FocusFirst();
        }
        _suppressFocusSound = false;
    }

    public int GetFocusedIndex()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i].HasFocus())
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Manually triggers the currently focused button.
    /// Useful for custom input handling (e.g. PlayerController inputs).
    /// </summary>
    public void TriggerFocusedOption()
    {
        foreach (var btn in _buttons)
        {
            if (btn.HasFocus())
            {
                btn.EmitSignal(BaseButton.SignalName.Pressed);
                return;
            }
        }
    }

    /// <summary>
    /// Enables or disables input interaction (Focus and Mouse) for all buttons in the menu.
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        foreach (var btn in _buttons)
        {
            btn.FocusMode = enabled ? FocusModeEnum.All : FocusModeEnum.None;
            btn.MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        }
    }

    /// <summary>
    /// Animates the menu collapsing horizontally into a thin sidebar.
    /// </summary>
    public async Task FoldToSidebar(string title)
    {

        // Fix for accumulation: If we already have a forced minimum width (from a previous unfold),
        // use that to prevent the menu from growing slightly on every fold/unfold cycle.
        if (CustomMinimumSize.X > 0)
        {
            _expandedWidth = CustomMinimumSize.X;
        }
        else
        {
            _expandedWidth = Size.X;
        }

        _sidebarLabel.Modulate = new Color(1, 1, 1, 0);
        _sidebarWrapper.Show();

        CustomMinimumSize = Size;

        // 1. Fade out the list content
        var fadeTween = CreateTween();
        fadeTween.TweenProperty(_listContainer, "modulate:a", 0.0f, 0.15f);
        await ToSignal(fadeTween, Tween.SignalName.Finished);
        _listContainer.Hide();

        // 2. Shrink the container width
        float targetWidth = 40.0f;
        var sizeTween = CreateTween().SetParallel(true);
        sizeTween.TweenProperty(this, "custom_minimum_size:x", targetWidth, 0.25f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        sizeTween.TweenProperty(this, "size:x", targetWidth, 0.25f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        
        await ToSignal(sizeTween, Tween.SignalName.Finished);

        // 3. Setup Label (Now that container is stable at small size)
        _sidebarLabel.Text = title;
        _sidebarLabel.RotationDegrees = 0; // Reset rotation to get accurate unrotated size
        _sidebarLabel.ResetSize();
        
        Vector2 labelSize = _sidebarLabel.Size;
        _sidebarLabel.PivotOffset = labelSize / 2;
        _sidebarLabel.RotationDegrees = -90;

        // Manual centering relative to the wrapper/container
        // We use the known target width (40) and current height to find the center.
        float centerX = targetWidth / 2.0f;
        float centerY = Size.Y / 2.0f;
        _sidebarLabel.Position = new Vector2(centerX, centerY) - (labelSize / 2);

        // 4. Fade in the label
        var labelTween = CreateTween();
        labelTween.TweenProperty(_sidebarLabel, "modulate:a", 1.0f, 0.2f);
        await ToSignal(labelTween, Tween.SignalName.Finished);
    }

    /// <summary>
    /// Animates the menu expanding back from a sidebar to the full list.
    /// </summary>
    public async Task Unfold()
    {
        // 1. Fade out label
        var labelTween = CreateTween();
        labelTween.TweenProperty(_sidebarLabel, "modulate:a", 0.0f, 0.1f);
        await ToSignal(labelTween, Tween.SignalName.Finished);
        _sidebarWrapper.Hide();

        // 2. Expand width first (keep content hidden to prevent layout snap)
        var expandTween = CreateTween().SetParallel(true);
        expandTween.TweenProperty(this, "custom_minimum_size:x", _expandedWidth, 0.25f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        expandTween.TweenProperty(this, "size:x", _expandedWidth, 0.25f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

        await ToSignal(expandTween, Tween.SignalName.Finished);

        // 3. Show and fade in list
        _listContainer.Modulate = new Color(1, 1, 1, 0);
        _listContainer.Show();
        var fadeTween = CreateTween();
        fadeTween.TweenProperty(_listContainer, "modulate:a", 1.0f, 0.15f);

        // Restore the width constraint to prevent shrinking if content is smaller than the original width.
        CustomMinimumSize = new Vector2(_expandedWidth, 0);
    }
}