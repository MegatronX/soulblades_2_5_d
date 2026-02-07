using Godot;

/// <summary>
/// Defines the visual appearance of a menu (backgrounds, colors, cursors).
/// </summary>
[GlobalClass]
public partial class MenuTheme : Resource
{
    [Export] public Texture2D BackgroundTexture { get; set; }
    [Export] public Texture2D SelectionCursor { get; set; }
    [Export] public Texture2D DefaultIcon { get; set; }
    [Export] public Godot.Collections.Dictionary<string, Texture2D> CommandIcons { get; set; } = new();
    [Export] public Texture2D ButtonBackground { get; set; }

    [Export] public Color TextColor { get; set; } = Colors.White;
    [Export] public Color SelectedTextColor { get; set; } = Colors.Yellow;

    [Export] public float OpenAnimationDuration { get; set; } = 0.25f;
    [Export] public float CloseAnimationDuration { get; set; } = 0.15f;

    [Export] public int ButtonHeight { get; set; } = 35;
    [Export] public int ButtonSpacing { get; set; } = 5;
    [Export] public int FontSize { get; set; } = 16;
}