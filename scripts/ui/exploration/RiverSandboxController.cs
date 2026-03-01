using Godot;
using System;

/// <summary>
/// Standalone river tuning sandbox.
/// Lets designers tune RiverChannel geometry + river water shader parameters
/// before embedding values into exploration scenes.
/// </summary>
public partial class RiverSandboxController : Control
{
    [ExportGroup("River")]
    [Export]
    public PackedScene RiverChannelScene { get; set; }

    [Export]
    public Material DefaultTerrainMaterial { get; set; }

    [Export]
    public Material DefaultBedMaterial { get; set; }

    [Export]
    public Material DefaultSmoothBankMaterial { get; set; }

    [Export]
    public Material DefaultGorgeWallMaterial { get; set; }

    [Export]
    public ShaderMaterial DefaultWaterMaterial { get; set; }

    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "0.05,4,0.01")]
    public float ScrollZoomStep { get; set; } = 0.75f;

    [Export(PropertyHint.Range, "2,120,0.1")]
    public float CameraMinDistance { get; set; } = 8.0f;

    [Export(PropertyHint.Range, "3,180,0.1")]
    public float CameraMaxDistance { get; set; } = 28.0f;

    [Export(PropertyHint.Range, "-4,8,0.01")]
    public float CameraFocusHeight { get; set; } = 0.75f;

    private const string FallbackRiverScenePath = "res://assets/scenes/exploration/props/RiverChannel.tscn";
    private const string FallbackWaterMaterialPath = "res://assets/resources/exploration/materials/forest_river_flow_material.tres";
    private const string FallbackTerrainMaterialPath = "res://assets/resources/exploration/materials/forest_river_bank_material.tres";
    private const string FallbackBedMaterialPath = "res://assets/resources/exploration/materials/forest_river_bed_material.tres";
    private const string FallbackGorgeWallMaterialPath = "res://assets/resources/exploration/materials/forest_river_gorge_wall_material.tres";

    private Camera3D _camera;
    private Node3D _riverAnchor;
    private RiverChannel _riverChannel;
    private ShaderMaterial _runtimeWaterMaterial;

    private Button _toggleUiButton;
    private PanelContainer _hudPanel;
    private Control _logPanel;
    private RichTextLabel _summaryLog;
    private Label _statusLabel;
    private OptionButton _styleOption;
    private HSlider _lengthSlider;
    private Label _lengthValue;
    private HSlider _waterWidthSlider;
    private Label _waterWidthValue;
    private HSlider _waterSurfaceYSlider;
    private Label _waterSurfaceYValue;
    private HSlider _channelDepthSlider;
    private Label _channelDepthValue;
    private HSlider _terrainPositiveSlider;
    private Label _terrainPositiveValue;
    private HSlider _terrainNegativeSlider;
    private Label _terrainNegativeValue;
    private HSlider _smoothRunSlider;
    private Label _smoothRunValue;
    private HSlider _gorgeWallThicknessSlider;
    private Label _gorgeWallThicknessValue;
    private HSlider _gorgeWallHeightSlider;
    private Label _gorgeWallHeightValue;

    private HSlider _flowSpeedSlider;
    private Label _flowSpeedValue;
    private HSlider _flowDirectionXSlider;
    private Label _flowDirectionXValue;
    private HSlider _flowDirectionYSlider;
    private Label _flowDirectionYValue;
    private HSlider _detailTilingSlider;
    private Label _detailTilingValue;
    private HSlider _waterAlphaSlider;
    private Label _waterAlphaValue;
    private HSlider _foamIntensitySlider;
    private Label _foamIntensityValue;
    private HSlider _edgeFoamWidthSlider;
    private Label _edgeFoamWidthValue;

    private Button _presetSmoothButton;
    private Button _presetGorgeButton;
    private Button _resetButton;
    private Button _copySnippetButton;

    private bool _uiHidden;
    private bool _suppressUiCallbacks;
    private float _cameraDistance = 15.0f;
    private Vector3 _cameraDirection = new Vector3(0.0f, 0.28f, 0.96f).Normalized();

    private Snapshot _defaults;

    private sealed class Snapshot
    {
        public RiverChannel.RiverBankStyle BankStyle;
        public float ChannelLength;
        public float WaterWidth;
        public float WaterSurfaceY;
        public float ChannelDepth;
        public float TerrainPositiveWidth;
        public float TerrainNegativeWidth;
        public float SmoothBankRun;
        public float GorgeWallThickness;
        public float GorgeWallHeight;
        public float FlowSpeed;
        public Vector2 FlowDirection;
        public float DetailTiling;
        public float WaterAlpha;
        public float FoamIntensity;
        public float EdgeFoamWidth;
    }

    public override void _Ready()
    {
        CacheNodes();
        EnsureRiverChannel();
        EnsureRuntimeWaterMaterial();
        CaptureDefaults();
        PopulateStyleOptions();
        WireUiEvents();
        SyncUiFromModel();
        InitializeCameraZoom();
        Log("River sandbox ready.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.F1)
        {
            ToggleUiVisibility();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex != MouseButton.WheelUp && mouseButton.ButtonIndex != MouseButton.WheelDown)
        {
            return;
        }

        var hoveredControl = GetViewport()?.GuiGetHoveredControl();
        if (hoveredControl is BaseButton or HSlider or OptionButton or ScrollContainer)
        {
            return;
        }

        float delta = mouseButton.ButtonIndex == MouseButton.WheelUp ? -ScrollZoomStep : ScrollZoomStep;
        AdjustCameraZoom(delta);
        GetViewport()?.SetInputAsHandled();
    }

    private void CacheNodes()
    {
        _camera = GetNodeOrNull<Camera3D>("SandboxWorld/Camera3D");
        _riverAnchor = GetNodeOrNull<Node3D>("SandboxWorld/RiverAnchor");

        _toggleUiButton = ResolveNode<Button>("UI/ToggleUiButton");
        _hudPanel = ResolveNode<PanelContainer>("UI/HUD");
        _logPanel = ResolveNode<Control>("UI/LogPanel");
        _summaryLog = ResolveNode<RichTextLabel>("UI/LogPanel/Body/LogLabel");
        _statusLabel = ResolveNode<Label>("UI/HUD/Scroll/Body/Header/StatusLabel", "UI/HUD/Body/Header/StatusLabel");

        _styleOption = ResolveNode<OptionButton>("UI/HUD/Scroll/Body/StyleRow/StyleOption", "UI/HUD/Body/StyleRow/StyleOption");
        _lengthSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/LengthRow/LengthSlider", "UI/HUD/Body/LengthRow/LengthSlider");
        _lengthValue = ResolveNode<Label>("UI/HUD/Scroll/Body/LengthRow/LengthValue", "UI/HUD/Body/LengthRow/LengthValue");
        _waterWidthSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/WaterWidthRow/WaterWidthSlider", "UI/HUD/Body/WaterWidthRow/WaterWidthSlider");
        _waterWidthValue = ResolveNode<Label>("UI/HUD/Scroll/Body/WaterWidthRow/WaterWidthValue", "UI/HUD/Body/WaterWidthRow/WaterWidthValue");
        _waterSurfaceYSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/WaterSurfaceYRow/WaterSurfaceYSlider", "UI/HUD/Body/WaterSurfaceYRow/WaterSurfaceYSlider");
        _waterSurfaceYValue = ResolveNode<Label>("UI/HUD/Scroll/Body/WaterSurfaceYRow/WaterSurfaceYValue", "UI/HUD/Body/WaterSurfaceYRow/WaterSurfaceYValue");
        _channelDepthSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/DepthRow/DepthSlider", "UI/HUD/Body/DepthRow/DepthSlider");
        _channelDepthValue = ResolveNode<Label>("UI/HUD/Scroll/Body/DepthRow/DepthValue", "UI/HUD/Body/DepthRow/DepthValue");
        _terrainPositiveSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/TerrainPositiveRow/TerrainPositiveSlider", "UI/HUD/Body/TerrainPositiveRow/TerrainPositiveSlider");
        _terrainPositiveValue = ResolveNode<Label>("UI/HUD/Scroll/Body/TerrainPositiveRow/TerrainPositiveValue", "UI/HUD/Body/TerrainPositiveRow/TerrainPositiveValue");
        _terrainNegativeSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/TerrainNegativeRow/TerrainNegativeSlider", "UI/HUD/Body/TerrainNegativeRow/TerrainNegativeSlider");
        _terrainNegativeValue = ResolveNode<Label>("UI/HUD/Scroll/Body/TerrainNegativeRow/TerrainNegativeValue", "UI/HUD/Body/TerrainNegativeRow/TerrainNegativeValue");
        _smoothRunSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/SmoothRunRow/SmoothRunSlider", "UI/HUD/Body/SmoothRunRow/SmoothRunSlider");
        _smoothRunValue = ResolveNode<Label>("UI/HUD/Scroll/Body/SmoothRunRow/SmoothRunValue", "UI/HUD/Body/SmoothRunRow/SmoothRunValue");
        _gorgeWallThicknessSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/GorgeThicknessRow/GorgeThicknessSlider", "UI/HUD/Body/GorgeThicknessRow/GorgeThicknessSlider");
        _gorgeWallThicknessValue = ResolveNode<Label>("UI/HUD/Scroll/Body/GorgeThicknessRow/GorgeThicknessValue", "UI/HUD/Body/GorgeThicknessRow/GorgeThicknessValue");
        _gorgeWallHeightSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/GorgeHeightRow/GorgeHeightSlider", "UI/HUD/Body/GorgeHeightRow/GorgeHeightSlider");
        _gorgeWallHeightValue = ResolveNode<Label>("UI/HUD/Scroll/Body/GorgeHeightRow/GorgeHeightValue", "UI/HUD/Body/GorgeHeightRow/GorgeHeightValue");

        _flowSpeedSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/FlowSpeedRow/FlowSpeedSlider", "UI/HUD/Body/FlowSpeedRow/FlowSpeedSlider");
        _flowSpeedValue = ResolveNode<Label>("UI/HUD/Scroll/Body/FlowSpeedRow/FlowSpeedValue", "UI/HUD/Body/FlowSpeedRow/FlowSpeedValue");
        _flowDirectionXSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/FlowDirectionXRow/FlowDirectionXSlider", "UI/HUD/Body/FlowDirectionXRow/FlowDirectionXSlider");
        _flowDirectionXValue = ResolveNode<Label>("UI/HUD/Scroll/Body/FlowDirectionXRow/FlowDirectionXValue", "UI/HUD/Body/FlowDirectionXRow/FlowDirectionXValue");
        _flowDirectionYSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/FlowDirectionYRow/FlowDirectionYSlider", "UI/HUD/Body/FlowDirectionYRow/FlowDirectionYSlider");
        _flowDirectionYValue = ResolveNode<Label>("UI/HUD/Scroll/Body/FlowDirectionYRow/FlowDirectionYValue", "UI/HUD/Body/FlowDirectionYRow/FlowDirectionYValue");
        _detailTilingSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/DetailTilingRow/DetailTilingSlider", "UI/HUD/Body/DetailTilingRow/DetailTilingSlider");
        _detailTilingValue = ResolveNode<Label>("UI/HUD/Scroll/Body/DetailTilingRow/DetailTilingValue", "UI/HUD/Body/DetailTilingRow/DetailTilingValue");
        _waterAlphaSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/WaterAlphaRow/WaterAlphaSlider", "UI/HUD/Body/WaterAlphaRow/WaterAlphaSlider");
        _waterAlphaValue = ResolveNode<Label>("UI/HUD/Scroll/Body/WaterAlphaRow/WaterAlphaValue", "UI/HUD/Body/WaterAlphaRow/WaterAlphaValue");
        _foamIntensitySlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/FoamIntensityRow/FoamIntensitySlider", "UI/HUD/Body/FoamIntensityRow/FoamIntensitySlider");
        _foamIntensityValue = ResolveNode<Label>("UI/HUD/Scroll/Body/FoamIntensityRow/FoamIntensityValue", "UI/HUD/Body/FoamIntensityRow/FoamIntensityValue");
        _edgeFoamWidthSlider = ResolveNode<HSlider>("UI/HUD/Scroll/Body/EdgeFoamWidthRow/EdgeFoamWidthSlider", "UI/HUD/Body/EdgeFoamWidthRow/EdgeFoamWidthSlider");
        _edgeFoamWidthValue = ResolveNode<Label>("UI/HUD/Scroll/Body/EdgeFoamWidthRow/EdgeFoamWidthValue", "UI/HUD/Body/EdgeFoamWidthRow/EdgeFoamWidthValue");

        _presetSmoothButton = ResolveNode<Button>("UI/HUD/Scroll/Body/ButtonsRow/PresetSmoothButton", "UI/HUD/Body/ButtonsRow/PresetSmoothButton");
        _presetGorgeButton = ResolveNode<Button>("UI/HUD/Scroll/Body/ButtonsRow/PresetGorgeButton", "UI/HUD/Body/ButtonsRow/PresetGorgeButton");
        _resetButton = ResolveNode<Button>("UI/HUD/Scroll/Body/ButtonsRow/ResetButton", "UI/HUD/Body/ButtonsRow/ResetButton");
        _copySnippetButton = ResolveNode<Button>("UI/HUD/Scroll/Body/ButtonsRow/CopySnippetButton", "UI/HUD/Body/ButtonsRow/CopySnippetButton");
    }

    private T ResolveNode<T>(params string[] paths) where T : Node
    {
        if (paths == null) return null;
        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            T node = GetNodeOrNull<T>(path);
            if (node != null) return node;
        }

        return null;
    }

    private void EnsureRiverChannel()
    {
        if (_riverAnchor == null)
        {
            GD.PrintErr("[RiverSandbox] Missing SandboxWorld/RiverAnchor.");
            return;
        }

        _riverChannel = _riverAnchor.GetNodeOrNull<RiverChannel>("RiverChannel");
        if (_riverChannel == null)
        {
            RiverChannelScene ??= GD.Load<PackedScene>(FallbackRiverScenePath);
            if (RiverChannelScene == null)
            {
                GD.PrintErr("[RiverSandbox] RiverChannel scene missing.");
                return;
            }

            var instance = RiverChannelScene.Instantiate<RiverChannel>();
            if (instance == null)
            {
                GD.PrintErr("[RiverSandbox] RiverChannel scene did not instantiate RiverChannel.");
                return;
            }

            instance.Name = "RiverChannel";
            _riverAnchor.AddChild(instance);
            _riverChannel = instance;
        }

        DefaultTerrainMaterial ??= GD.Load<Material>(FallbackTerrainMaterialPath);
        DefaultBedMaterial ??= GD.Load<Material>(FallbackBedMaterialPath);
        DefaultGorgeWallMaterial ??= GD.Load<Material>(FallbackGorgeWallMaterialPath);
        DefaultSmoothBankMaterial ??= DefaultTerrainMaterial;

        if (_riverChannel.TerrainMaterial == null) _riverChannel.TerrainMaterial = DefaultTerrainMaterial;
        if (_riverChannel.BedMaterial == null) _riverChannel.BedMaterial = DefaultBedMaterial;
        if (_riverChannel.SmoothBankMaterial == null) _riverChannel.SmoothBankMaterial = DefaultSmoothBankMaterial;
        if (_riverChannel.GorgeWallMaterial == null) _riverChannel.GorgeWallMaterial = DefaultGorgeWallMaterial;
    }

    private void EnsureRuntimeWaterMaterial()
    {
        if (_riverChannel == null) return;

        DefaultWaterMaterial ??= GD.Load<ShaderMaterial>(FallbackWaterMaterialPath);
        var source = _riverChannel.WaterMaterial as ShaderMaterial ?? DefaultWaterMaterial;
        if (source == null)
        {
            GD.PrintErr("[RiverSandbox] Missing shader water material.");
            return;
        }

        _runtimeWaterMaterial = source.Duplicate() as ShaderMaterial;
        if (_runtimeWaterMaterial == null)
        {
            _runtimeWaterMaterial = source;
        }

        _riverChannel.WaterMaterial = _runtimeWaterMaterial;
        _riverChannel.RefreshGeometry();
    }

    private void CaptureDefaults()
    {
        if (_riverChannel == null) return;

        _defaults = new Snapshot
        {
            BankStyle = _riverChannel.BankStyle,
            ChannelLength = _riverChannel.ChannelLength,
            WaterWidth = _riverChannel.WaterWidth,
            WaterSurfaceY = _riverChannel.WaterSurfaceY,
            ChannelDepth = _riverChannel.ChannelDepth,
            TerrainPositiveWidth = _riverChannel.TerrainPositiveWidth,
            TerrainNegativeWidth = _riverChannel.TerrainNegativeWidth,
            SmoothBankRun = _riverChannel.SmoothBankRun,
            GorgeWallThickness = _riverChannel.GorgeWallThickness,
            GorgeWallHeight = _riverChannel.GorgeWallHeight,
            FlowSpeed = GetShaderFloat("flow_speed", 0.2f),
            FlowDirection = GetShaderVector2("flow_direction", new Vector2(0.0f, -1.0f)),
            DetailTiling = GetShaderFloat("detail_tiling", 11.0f),
            WaterAlpha = GetShaderFloat("water_alpha", 0.84f),
            FoamIntensity = GetShaderFloat("foam_intensity", 0.55f),
            EdgeFoamWidth = GetShaderFloat("edge_foam_width", 0.095f)
        };
    }

    private void PopulateStyleOptions()
    {
        if (_styleOption == null) return;
        _styleOption.Clear();
        _styleOption.AddItem("Smooth Banks", (int)RiverChannel.RiverBankStyle.SmoothBanks);
        _styleOption.AddItem("Rocky Gorge", (int)RiverChannel.RiverBankStyle.RockyGorge);
    }

    private void WireUiEvents()
    {
        if (_toggleUiButton != null)
        {
            _toggleUiButton.Pressed += ToggleUiVisibility;
        }

        if (_styleOption != null) _styleOption.ItemSelected += _ => ApplyUiToRiver();

        WireSlider(_lengthSlider, _ => ApplyUiToRiver());
        WireSlider(_waterWidthSlider, _ => ApplyUiToRiver());
        WireSlider(_waterSurfaceYSlider, _ => ApplyUiToRiver());
        WireSlider(_channelDepthSlider, _ => ApplyUiToRiver());
        WireSlider(_terrainPositiveSlider, _ => ApplyUiToRiver());
        WireSlider(_terrainNegativeSlider, _ => ApplyUiToRiver());
        WireSlider(_smoothRunSlider, _ => ApplyUiToRiver());
        WireSlider(_gorgeWallThicknessSlider, _ => ApplyUiToRiver());
        WireSlider(_gorgeWallHeightSlider, _ => ApplyUiToRiver());

        WireSlider(_flowSpeedSlider, _ => ApplyUiToRiver());
        WireSlider(_flowDirectionXSlider, _ => ApplyUiToRiver());
        WireSlider(_flowDirectionYSlider, _ => ApplyUiToRiver());
        WireSlider(_detailTilingSlider, _ => ApplyUiToRiver());
        WireSlider(_waterAlphaSlider, _ => ApplyUiToRiver());
        WireSlider(_foamIntensitySlider, _ => ApplyUiToRiver());
        WireSlider(_edgeFoamWidthSlider, _ => ApplyUiToRiver());

        if (_presetSmoothButton != null) _presetSmoothButton.Pressed += ApplySmoothPreset;
        if (_presetGorgeButton != null) _presetGorgeButton.Pressed += ApplyGorgePreset;
        if (_resetButton != null) _resetButton.Pressed += ResetToDefaults;
        if (_copySnippetButton != null) _copySnippetButton.Pressed += CopySnippetToClipboard;
    }

    private static void WireSlider(HSlider slider, Action<double> callback)
    {
        if (slider == null || callback == null) return;
        slider.ValueChanged += value => callback(value);
    }

    private void SyncUiFromModel()
    {
        if (_riverChannel == null) return;

        _suppressUiCallbacks = true;

        if (_styleOption != null)
        {
            int selected = _styleOption.GetItemIndex((int)_riverChannel.BankStyle);
            _styleOption.Selected = selected >= 0 ? selected : 0;
        }

        SetSliderValue(_lengthSlider, _riverChannel.ChannelLength);
        SetSliderValue(_waterWidthSlider, _riverChannel.WaterWidth);
        SetSliderValue(_waterSurfaceYSlider, _riverChannel.WaterSurfaceY);
        SetSliderValue(_channelDepthSlider, _riverChannel.ChannelDepth);
        SetSliderValue(_terrainPositiveSlider, _riverChannel.TerrainPositiveWidth);
        SetSliderValue(_terrainNegativeSlider, _riverChannel.TerrainNegativeWidth);
        SetSliderValue(_smoothRunSlider, _riverChannel.SmoothBankRun);
        SetSliderValue(_gorgeWallThicknessSlider, _riverChannel.GorgeWallThickness);
        SetSliderValue(_gorgeWallHeightSlider, _riverChannel.GorgeWallHeight);

        Vector2 flowDirection = GetShaderVector2("flow_direction", new Vector2(0.0f, -1.0f));
        SetSliderValue(_flowSpeedSlider, GetShaderFloat("flow_speed", 0.2f));
        SetSliderValue(_flowDirectionXSlider, flowDirection.X);
        SetSliderValue(_flowDirectionYSlider, flowDirection.Y);
        SetSliderValue(_detailTilingSlider, GetShaderFloat("detail_tiling", 11.0f));
        SetSliderValue(_waterAlphaSlider, GetShaderFloat("water_alpha", 0.84f));
        SetSliderValue(_foamIntensitySlider, GetShaderFloat("foam_intensity", 0.55f));
        SetSliderValue(_edgeFoamWidthSlider, GetShaderFloat("edge_foam_width", 0.095f));

        _suppressUiCallbacks = false;

        UpdateValueLabels();
        UpdateStatusLabel();
    }

    private static void SetSliderValue(HSlider slider, float value)
    {
        if (slider == null) return;
        slider.Value = value;
    }

    private void ApplyUiToRiver()
    {
        if (_suppressUiCallbacks || _riverChannel == null) return;

        if (_styleOption != null)
        {
            int styleId = _styleOption.GetSelectedId();
            _riverChannel.BankStyle = Enum.IsDefined(typeof(RiverChannel.RiverBankStyle), styleId)
                ? (RiverChannel.RiverBankStyle)styleId
                : RiverChannel.RiverBankStyle.SmoothBanks;
        }

        _riverChannel.ChannelLength = GetSliderFloat(_lengthSlider, _riverChannel.ChannelLength);
        _riverChannel.WaterWidth = GetSliderFloat(_waterWidthSlider, _riverChannel.WaterWidth);
        _riverChannel.WaterSurfaceY = GetSliderFloat(_waterSurfaceYSlider, _riverChannel.WaterSurfaceY);
        _riverChannel.ChannelDepth = GetSliderFloat(_channelDepthSlider, _riverChannel.ChannelDepth);
        _riverChannel.TerrainPositiveWidth = GetSliderFloat(_terrainPositiveSlider, _riverChannel.TerrainPositiveWidth);
        _riverChannel.TerrainNegativeWidth = GetSliderFloat(_terrainNegativeSlider, _riverChannel.TerrainNegativeWidth);
        _riverChannel.SmoothBankRun = GetSliderFloat(_smoothRunSlider, _riverChannel.SmoothBankRun);
        _riverChannel.GorgeWallThickness = GetSliderFloat(_gorgeWallThicknessSlider, _riverChannel.GorgeWallThickness);
        _riverChannel.GorgeWallHeight = GetSliderFloat(_gorgeWallHeightSlider, _riverChannel.GorgeWallHeight);

        if (_runtimeWaterMaterial != null)
        {
            _runtimeWaterMaterial.SetShaderParameter("flow_speed", GetSliderFloat(_flowSpeedSlider, GetShaderFloat("flow_speed", 0.2f)));
            _runtimeWaterMaterial.SetShaderParameter("flow_direction", new Vector2(
                GetSliderFloat(_flowDirectionXSlider, 0.0f),
                GetSliderFloat(_flowDirectionYSlider, -1.0f)));
            _runtimeWaterMaterial.SetShaderParameter("detail_tiling", GetSliderFloat(_detailTilingSlider, GetShaderFloat("detail_tiling", 11.0f)));
            _runtimeWaterMaterial.SetShaderParameter("water_alpha", GetSliderFloat(_waterAlphaSlider, GetShaderFloat("water_alpha", 0.84f)));
            _runtimeWaterMaterial.SetShaderParameter("foam_intensity", GetSliderFloat(_foamIntensitySlider, GetShaderFloat("foam_intensity", 0.55f)));
            _runtimeWaterMaterial.SetShaderParameter("edge_foam_width", GetSliderFloat(_edgeFoamWidthSlider, GetShaderFloat("edge_foam_width", 0.095f)));
        }

        _riverChannel.RefreshGeometry();
        UpdateValueLabels();
        UpdateStatusLabel();
    }

    private static float GetSliderFloat(HSlider slider, float fallback)
    {
        return slider == null ? fallback : (float)slider.Value;
    }

    private void UpdateValueLabels()
    {
        SetValueLabel(_lengthValue, _lengthSlider, "0.00");
        SetValueLabel(_waterWidthValue, _waterWidthSlider, "0.00");
        SetValueLabel(_waterSurfaceYValue, _waterSurfaceYSlider, "0.00");
        SetValueLabel(_channelDepthValue, _channelDepthSlider, "0.00");
        SetValueLabel(_terrainPositiveValue, _terrainPositiveSlider, "0.00");
        SetValueLabel(_terrainNegativeValue, _terrainNegativeSlider, "0.00");
        SetValueLabel(_smoothRunValue, _smoothRunSlider, "0.00");
        SetValueLabel(_gorgeWallThicknessValue, _gorgeWallThicknessSlider, "0.00");
        SetValueLabel(_gorgeWallHeightValue, _gorgeWallHeightSlider, "0.00");
        SetValueLabel(_flowSpeedValue, _flowSpeedSlider, "0.000");
        SetValueLabel(_flowDirectionXValue, _flowDirectionXSlider, "0.00");
        SetValueLabel(_flowDirectionYValue, _flowDirectionYSlider, "0.00");
        SetValueLabel(_detailTilingValue, _detailTilingSlider, "0.00");
        SetValueLabel(_waterAlphaValue, _waterAlphaSlider, "0.000");
        SetValueLabel(_foamIntensityValue, _foamIntensitySlider, "0.000");
        SetValueLabel(_edgeFoamWidthValue, _edgeFoamWidthSlider, "0.000");
    }

    private static void SetValueLabel(Label label, HSlider slider, string format)
    {
        if (label == null || slider == null) return;
        label.Text = slider.Value.ToString(format);
    }

    private void UpdateStatusLabel()
    {
        if (_statusLabel == null || _riverChannel == null) return;
        _statusLabel.Text = $"Style: {_riverChannel.BankStyle} | Flow: {GetShaderVector2("flow_direction", new Vector2(0.0f, -1.0f))}";
    }

    private void ApplySmoothPreset()
    {
        if (_riverChannel == null) return;
        _suppressUiCallbacks = true;
        _styleOption?.Select(_styleOption.GetItemIndex((int)RiverChannel.RiverBankStyle.SmoothBanks));
        SetSliderValue(_waterSurfaceYSlider, -0.08f);
        SetSliderValue(_channelDepthSlider, 0.55f);
        SetSliderValue(_smoothRunSlider, 2.1f);
        SetSliderValue(_gorgeWallThicknessSlider, 0.5f);
        SetSliderValue(_gorgeWallHeightSlider, 1.2f);
        _suppressUiCallbacks = false;
        ApplyUiToRiver();
        Log("Applied Smooth Banks preset.");
    }

    private void ApplyGorgePreset()
    {
        if (_riverChannel == null) return;
        _suppressUiCallbacks = true;
        _styleOption?.Select(_styleOption.GetItemIndex((int)RiverChannel.RiverBankStyle.RockyGorge));
        SetSliderValue(_waterSurfaceYSlider, -0.32f);
        SetSliderValue(_channelDepthSlider, 1.65f);
        SetSliderValue(_smoothRunSlider, 1.2f);
        SetSliderValue(_gorgeWallThicknessSlider, 1.1f);
        SetSliderValue(_gorgeWallHeightSlider, 2.8f);
        _suppressUiCallbacks = false;
        ApplyUiToRiver();
        Log("Applied Rocky Gorge preset.");
    }

    private void ResetToDefaults()
    {
        if (_defaults == null || _riverChannel == null) return;

        _suppressUiCallbacks = true;
        _riverChannel.BankStyle = _defaults.BankStyle;
        _riverChannel.ChannelLength = _defaults.ChannelLength;
        _riverChannel.WaterWidth = _defaults.WaterWidth;
        _riverChannel.WaterSurfaceY = _defaults.WaterSurfaceY;
        _riverChannel.ChannelDepth = _defaults.ChannelDepth;
        _riverChannel.TerrainPositiveWidth = _defaults.TerrainPositiveWidth;
        _riverChannel.TerrainNegativeWidth = _defaults.TerrainNegativeWidth;
        _riverChannel.SmoothBankRun = _defaults.SmoothBankRun;
        _riverChannel.GorgeWallThickness = _defaults.GorgeWallThickness;
        _riverChannel.GorgeWallHeight = _defaults.GorgeWallHeight;

        if (_runtimeWaterMaterial != null)
        {
            _runtimeWaterMaterial.SetShaderParameter("flow_speed", _defaults.FlowSpeed);
            _runtimeWaterMaterial.SetShaderParameter("flow_direction", _defaults.FlowDirection);
            _runtimeWaterMaterial.SetShaderParameter("detail_tiling", _defaults.DetailTiling);
            _runtimeWaterMaterial.SetShaderParameter("water_alpha", _defaults.WaterAlpha);
            _runtimeWaterMaterial.SetShaderParameter("foam_intensity", _defaults.FoamIntensity);
            _runtimeWaterMaterial.SetShaderParameter("edge_foam_width", _defaults.EdgeFoamWidth);
        }

        _riverChannel.RefreshGeometry();
        _suppressUiCallbacks = false;
        SyncUiFromModel();
        Log("Reset river tuning to startup defaults.");
    }

    private void CopySnippetToClipboard()
    {
        if (_riverChannel == null) return;

        Vector2 flowDirection = GetShaderVector2("flow_direction", new Vector2(0.0f, -1.0f));
        string snippet = string.Join('\n', new[]
        {
            "[RiverSandbox Snippet]",
            $"BankStyle = {(int)_riverChannel.BankStyle}",
            $"ChannelLength = {FormatFloat(_riverChannel.ChannelLength)}",
            $"WaterWidth = {FormatFloat(_riverChannel.WaterWidth)}",
            $"WaterSurfaceY = {FormatFloat(_riverChannel.WaterSurfaceY)}",
            $"ChannelDepth = {FormatFloat(_riverChannel.ChannelDepth)}",
            $"TerrainPositiveWidth = {FormatFloat(_riverChannel.TerrainPositiveWidth)}",
            $"TerrainNegativeWidth = {FormatFloat(_riverChannel.TerrainNegativeWidth)}",
            $"SmoothBankRun = {FormatFloat(_riverChannel.SmoothBankRun)}",
            $"GorgeWallThickness = {FormatFloat(_riverChannel.GorgeWallThickness)}",
            $"GorgeWallHeight = {FormatFloat(_riverChannel.GorgeWallHeight)}",
            $"shader_parameter/flow_speed = {FormatFloat(GetShaderFloat("flow_speed", 0.2f))}",
            $"shader_parameter/flow_direction = Vector2({FormatFloat(flowDirection.X)}, {FormatFloat(flowDirection.Y)})",
            $"shader_parameter/detail_tiling = {FormatFloat(GetShaderFloat("detail_tiling", 11.0f))}",
            $"shader_parameter/water_alpha = {FormatFloat(GetShaderFloat("water_alpha", 0.84f))}",
            $"shader_parameter/foam_intensity = {FormatFloat(GetShaderFloat("foam_intensity", 0.55f))}",
            $"shader_parameter/edge_foam_width = {FormatFloat(GetShaderFloat("edge_foam_width", 0.095f))}"
        });

        DisplayServer.ClipboardSet(snippet);
        Log("Copied river tuning snippet to clipboard.");
    }

    private static string FormatFloat(float value) => value.ToString("0.###");

    private float GetShaderFloat(string parameterName, float fallback)
    {
        if (_runtimeWaterMaterial == null) return fallback;
        Variant variant = _runtimeWaterMaterial.GetShaderParameter(parameterName);
        return variant.VariantType == Variant.Type.Nil ? fallback : variant.AsSingle();
    }

    private Vector2 GetShaderVector2(string parameterName, Vector2 fallback)
    {
        if (_runtimeWaterMaterial == null) return fallback;
        Variant variant = _runtimeWaterMaterial.GetShaderParameter(parameterName);
        return variant.VariantType == Variant.Type.Nil ? fallback : variant.AsVector2();
    }

    private void ToggleUiVisibility()
    {
        _uiHidden = !_uiHidden;
        if (_hudPanel != null) _hudPanel.Visible = !_uiHidden;
        if (_logPanel != null) _logPanel.Visible = !_uiHidden;
        if (_toggleUiButton != null) _toggleUiButton.Text = _uiHidden ? "Show Tuning UI" : "Hide Tuning UI";
    }

    private void InitializeCameraZoom()
    {
        if (_camera == null) return;
        Vector3 focus = GetCameraFocusPoint();
        Vector3 offset = _camera.GlobalPosition - focus;
        if (offset.LengthSquared() <= 0.0001f)
        {
            offset = new Vector3(0.0f, 3.8f, 13.0f);
        }

        _cameraDirection = offset.Normalized();
        _cameraDistance = offset.Length();
        ClampCameraDistance();
        ApplyCameraZoomPosition();
    }

    private Vector3 GetCameraFocusPoint()
    {
        Vector3 anchor = _riverAnchor?.GlobalPosition ?? Vector3.Zero;
        anchor.Y += CameraFocusHeight;
        return anchor;
    }

    private void AdjustCameraZoom(float delta)
    {
        if (_camera == null) return;
        _cameraDistance += delta;
        ClampCameraDistance();
        ApplyCameraZoomPosition();
    }

    private void ClampCameraDistance()
    {
        float minDistance = Mathf.Max(0.5f, CameraMinDistance);
        float maxDistance = Mathf.Max(minDistance, CameraMaxDistance);
        _cameraDistance = Mathf.Clamp(_cameraDistance, minDistance, maxDistance);
    }

    private void ApplyCameraZoomPosition()
    {
        if (_camera == null) return;
        Vector3 focus = GetCameraFocusPoint();
        _camera.GlobalPosition = focus + (_cameraDirection * _cameraDistance);
        _camera.LookAt(focus, Vector3.Up);
    }

    private void Log(string message)
    {
        if (_summaryLog == null || string.IsNullOrWhiteSpace(message)) return;
        string timestamp = Time.GetDatetimeStringFromSystem();
        _summaryLog.Text = $"{timestamp}: {message}\n{_summaryLog.Text}";
    }
}
