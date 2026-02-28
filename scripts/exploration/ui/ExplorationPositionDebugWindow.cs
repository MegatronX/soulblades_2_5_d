using Godot;
using System.Linq;

/// <summary>
/// Small toggleable overlay that shows the current player 3D position in exploration maps.
/// </summary>
[GlobalClass]
public partial class ExplorationPositionDebugWindow : Control
{
    [Export]
    public Key ToggleKey { get; set; } = Key.F8;

    [Export]
    public bool StartVisible { get; set; } = false;

    [Export]
    public int FontSize { get; set; } = 16;

    [Export(PropertyHint.Range, "0.01,0.5,0.01")]
    public float RefreshIntervalSeconds { get; set; } = 0.05f;

    private PanelContainer _panel;
    private Label _titleLabel;
    private Label _positionLabel;
    private double _refreshAccumulator;

    public override void _Ready()
    {
        BuildUi();
        _panel.Visible = StartVisible;
        RefreshText();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed || keyEvent.Echo) return;
        if (keyEvent.Keycode != ToggleKey) return;

        _panel.Visible = !_panel.Visible;
        if (_panel.Visible)
        {
            RefreshText();
        }
    }

    public override void _Process(double delta)
    {
        if (_panel == null || !_panel.Visible) return;

        _refreshAccumulator += delta;
        if (_refreshAccumulator < RefreshIntervalSeconds) return;
        _refreshAccumulator = 0;
        RefreshText();
    }

    private void BuildUi()
    {
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0;
        _panel.AnchorTop = 0;
        _panel.AnchorRight = 0;
        _panel.AnchorBottom = 0;
        _panel.OffsetLeft = 24;
        _panel.OffsetTop = 60;
        _panel.OffsetRight = 360;
        _panel.OffsetBottom = 134;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.52f),
            BorderColor = new Color(1f, 1f, 1f, 0.28f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 4);
        _panel.AddChild(content);

        _titleLabel = new Label
        {
            Text = $"Position Debug ({ToggleKey})"
        };
        _titleLabel.LabelSettings = new LabelSettings
        {
            FontSize = Mathf.Max(10, FontSize),
            FontColor = Colors.White,
            OutlineSize = 1,
            OutlineColor = Colors.Black
        };
        content.AddChild(_titleLabel);

        _positionLabel = new Label();
        _positionLabel.LabelSettings = new LabelSettings
        {
            FontSize = Mathf.Max(10, FontSize),
            FontColor = new Color(0.88f, 0.94f, 1f),
            OutlineSize = 1,
            OutlineColor = Colors.Black
        };
        content.AddChild(_positionLabel);
    }

    private void RefreshText()
    {
        if (_positionLabel == null) return;

        var mapController = ResolveMapController();
        if (mapController == null)
        {
            _positionLabel.Text = "Map controller not found.";
            return;
        }

        CharacterBody3D actor = mapController.PlayerActor;
        if (actor == null || !GodotObject.IsInstanceValid(actor))
        {
            actor = GetTree()?
                .GetNodesInGroup(GameGroups.PlayerCharacters)
                .OfType<CharacterBody3D>()
                .FirstOrDefault(node => node.GetTree() == GetTree());
        }

        if (actor == null || !GodotObject.IsInstanceValid(actor))
        {
            _positionLabel.Text = "Player actor not found.";
            return;
        }

        Vector3 p = actor.GlobalPosition;
        _positionLabel.Text = $"X: {p.X:0.00}\nY: {p.Y:0.00}\nZ: {p.Z:0.00}";
    }

    private ExplorationMapController ResolveMapController()
    {
        if (GetTree()?.CurrentScene is ExplorationMapController sceneController)
        {
            return sceneController;
        }

        Node cursor = this;
        while (cursor != null)
        {
            if (cursor is ExplorationMapController controller)
            {
                return controller;
            }
            cursor = cursor.GetParent();
        }

        return null;
    }
}
