using Godot;

/// <summary>
/// Represents a single "card" in the turn order UI, displaying one combatant.
/// </summary>
public partial class TurnOrderCard : PanelContainer
{
    public TurnManager.TurnData Data { get; private set; }

    [Export]
    private Vector2 _minimumCardSize = new Vector2(85, 40);

    [Export]
    public Vector2 _maximumCardSize = new Vector2(125, 58);

    [Export]
    private Label _nameLabel;

    [Export]
    private TextureRect _iconRect;

    private Panel _highlightPanel;

    public override void _Ready()
    {
        CustomMinimumSize = _minimumCardSize;
        
        // Reset anchors to TopLeft to prevent layout overrides and warnings when setting Size manually.
        SetAnchorsPreset(LayoutPreset.TopLeft);

        // To enforce a maximum size, we set the control's size directly
        // and disable the expand flag so it doesn't grow beyond this size.
        Size = _maximumCardSize;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin; // Prevent horizontal expansion
        SizeFlagsVertical = SizeFlags.ShrinkBegin;   // Prevent vertical expansion

        // This is the key: it prevents children from forcing the container to grow.
        ClipContents = true;

        _nameLabel ??= GetNode<Label>("Label");
        _iconRect ??= GetNode<TextureRect>("Icon");

        // This is the most robust fix. It forces the icon to respect its container's bounds.
        if (_iconRect != null)
        {
            _iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        }

        SetupHighlightOverlay();
        SetProcess(false); // Disable processing by default to save performance
    }

    private void SetupHighlightOverlay()
    {
        _highlightPanel = new Panel();
        _highlightPanel.Name = "HighlightOverlay";
        _highlightPanel.MouseFilter = MouseFilterEnum.Ignore;
        
        // Create a stylebox for the border
        var styleBox = new StyleBoxFlat();
        styleBox.DrawCenter = false; // Transparent center
        styleBox.BorderWidthLeft = 3;
        styleBox.BorderWidthTop = 3;
        styleBox.BorderWidthRight = 3;
        styleBox.BorderWidthBottom = 3;
        styleBox.BorderColor = new Color(1f, 0.8f, 0.2f); // Gold/Yellow
        styleBox.CornerRadiusTopLeft = 4;
        styleBox.CornerRadiusTopRight = 4;
        styleBox.CornerRadiusBottomRight = 4;
        styleBox.CornerRadiusBottomLeft = 4;
        
        _highlightPanel.AddThemeStyleboxOverride("panel", styleBox);
        _highlightPanel.Hide();
        
        // Add as child. Since it's a PanelContainer, it will be resized to fit.
        // Adding it last ensures it draws on top of other children.
        AddChild(_highlightPanel);
    }

    public void SetData(TurnManager.TurnData data)
    {
        Data = data;
        Name = $"{data.Combatant.Name}_{data.TickValue}"; // Give the card a unique name for debugging.

        // Default to an empty texture.
        _iconRect.Texture = null;

        // Attempt to get the presentation data from the combatant node.
        if (data.Combatant is BaseCharacter character)
        {
            _nameLabel.Text = character.PresentationData?.DisplayName ?? data.Combatant.Name;
            _iconRect.Texture = character.PresentationData?.TurnQueueIcon;
        }
        else
        {
            // Fallback for nodes that don't have presentation data.
            _nameLabel.Text = data.Combatant.Name;
        }
    }

    /// <summary>
    /// Sets the visual highlight state of the card.
    /// </summary>
    /// <param name="isHighlighted">True to highlight, false to return to normal.</param>
    public void SetHighlight(bool isHighlighted)
    {
        if (isHighlighted)
        {
            _highlightPanel.Show();
            SetProcess(true);
        }
        else
        {
            _highlightPanel.Hide();
            SetProcess(false);
            _highlightPanel.Modulate = Colors.White;
        }
    }

    public override void _Process(double delta)
    {
        // Use global time to ensure all highlighted cards pulse in perfect sync.
        // Speed ~5.0 corresponds to the previous tween duration (approx 1.2s period).
        float time = Time.GetTicksMsec() / 1000.0f;
        float pulse = (Mathf.Sin(time * 5.0f) + 1.0f) * 0.5f; // 0 to 1
        float alpha = 0.3f + (pulse * 0.7f); // 0.3 to 1.0
        
        _highlightPanel.Modulate = new Color(1, 1, 1, alpha);
    }
}