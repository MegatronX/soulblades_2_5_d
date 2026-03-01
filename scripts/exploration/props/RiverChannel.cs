using Godot;
using System;

/// <summary>
/// Reusable exploration river geometry supporting two styles:
/// 1) Smooth banks (mostly level rivers)
/// 2) Rocky gorges (deeper channels with walls)
///
/// The channel is oriented along local +X/-X (length), with width on local Z.
/// Use this as the visual ground replacement around the river corridor so the
/// river can use distinct textures and visible depth.
/// </summary>
[Tool]
[GlobalClass]
public partial class RiverChannel : Node3D
{
    public enum RiverBankStyle
    {
        SmoothBanks = 0,
        RockyGorge = 1
    }

    [ExportGroup("Shape")]
    [Export]
    public RiverBankStyle BankStyle { get; set; } = RiverBankStyle.SmoothBanks;

    [Export(PropertyHint.Range, "2,256,0.1,suffix:m")]
    public float ChannelLength { get; set; } = 42.0f;

    [Export(PropertyHint.Range, "0.5,64,0.05,suffix:m")]
    public float WaterWidth { get; set; } = 8.0f;

    [Export(PropertyHint.Range, "-8,8,0.01,suffix:m")]
    public float WaterSurfaceY { get; set; } = -0.08f;

    [Export(PropertyHint.Range, "0.05,20,0.05,suffix:m")]
    public float ChannelDepth { get; set; } = 1.25f;

    [Export(PropertyHint.Range, "0,128,0.1,suffix:m")]
    public float TerrainPositiveWidth { get; set; } = 17.0f;

    [Export(PropertyHint.Range, "0,128,0.1,suffix:m")]
    public float TerrainNegativeWidth { get; set; } = 17.0f;

    [ExportGroup("Smooth Banks")]
    [Export(PropertyHint.Range, "0.1,16,0.05,suffix:m")]
    public float SmoothBankRun { get; set; } = 2.0f;

    [Export(PropertyHint.Range, "0.01,2,0.01,suffix:m")]
    public float SmoothBankThickness { get; set; } = 0.22f;

    [ExportGroup("Rocky Gorge")]
    [Export(PropertyHint.Range, "0.05,8,0.05,suffix:m")]
    public float GorgeWallThickness { get; set; } = 0.85f;

    [Export(PropertyHint.Range, "0.1,16,0.05,suffix:m")]
    public float GorgeWallHeight { get; set; } = 2.4f;

    [ExportGroup("Bed")]
    [Export(PropertyHint.Range, "0.01,2,0.01,suffix:m")]
    public float BedThickness { get; set; } = 0.12f;

    [ExportGroup("Collision")]
    [Export]
    public bool GenerateCollision { get; set; } = true;

    [Export(PropertyHint.Range, "0.05,4,0.05,suffix:m")]
    public float TerrainCollisionThickness { get; set; } = 1.0f;

    [Export]
    public bool BlockRiverCrossing { get; set; } = false;

    [Export(PropertyHint.Range, "0.1,12,0.1,suffix:m")]
    public float RiverBlockerHeight { get; set; } = 2.2f;

    [ExportGroup("Materials")]
    [Export]
    public Material TerrainMaterial { get; set; }

    [Export]
    public Material WaterMaterial { get; set; }

    [Export]
    public Material BedMaterial { get; set; }

    [Export]
    public Material SmoothBankMaterial { get; set; }

    [Export]
    public Material GorgeWallMaterial { get; set; }

    private Node3D _generatedRoot;
    private MeshInstance3D _terrainPositive;
    private MeshInstance3D _terrainNegative;
    private MeshInstance3D _waterSurface;
    private MeshInstance3D _riverBed;
    private MeshInstance3D _smoothBankPositive;
    private MeshInstance3D _smoothBankNegative;
    private MeshInstance3D _gorgeWallPositive;
    private MeshInstance3D _gorgeWallNegative;

    private StaticBody3D _collisionBody;
    private CollisionShape3D _terrainPositiveCollision;
    private CollisionShape3D _terrainNegativeCollision;
    private CollisionShape3D _smoothPositiveCollision;
    private CollisionShape3D _smoothNegativeCollision;
    private CollisionShape3D _gorgePositiveCollision;
    private CollisionShape3D _gorgeNegativeCollision;
    private CollisionShape3D _riverBlockerCollision;

    private int _lastConfigHash;

    public override void _Ready()
    {
        RefreshGeometry();
        SetProcess(Engine.IsEditorHint());
    }

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;

        int hash = ComputeConfigHash();
        if (hash == _lastConfigHash) return;

        RefreshGeometry();
    }

    public void RefreshGeometry()
    {
        EnsureGeneratedNodes();
        RebuildGeometry();
        _lastConfigHash = ComputeConfigHash();
    }

    private int ComputeConfigHash()
    {
        var hash = new HashCode();
        hash.Add((int)BankStyle);
        hash.Add(ChannelLength);
        hash.Add(WaterWidth);
        hash.Add(WaterSurfaceY);
        hash.Add(ChannelDepth);
        hash.Add(TerrainPositiveWidth);
        hash.Add(TerrainNegativeWidth);
        hash.Add(SmoothBankRun);
        hash.Add(SmoothBankThickness);
        hash.Add(GorgeWallThickness);
        hash.Add(GorgeWallHeight);
        hash.Add(BedThickness);
        hash.Add(GenerateCollision);
        hash.Add(TerrainCollisionThickness);
        hash.Add(BlockRiverCrossing);
        hash.Add(RiverBlockerHeight);
        hash.Add(TerrainMaterial);
        hash.Add(WaterMaterial);
        hash.Add(BedMaterial);
        hash.Add(SmoothBankMaterial);
        hash.Add(GorgeWallMaterial);
        return hash.ToHashCode();
    }

    private void EnsureGeneratedNodes()
    {
        _generatedRoot = EnsureChildNode<Node3D>(this, "__GeneratedRiverChannel");

        _terrainPositive = EnsureMeshNode(_generatedRoot, "TerrainPositive");
        _terrainNegative = EnsureMeshNode(_generatedRoot, "TerrainNegative");
        _waterSurface = EnsureMeshNode(_generatedRoot, "WaterSurface");
        _riverBed = EnsureMeshNode(_generatedRoot, "RiverBed");
        _smoothBankPositive = EnsureMeshNode(_generatedRoot, "SmoothBankPositive");
        _smoothBankNegative = EnsureMeshNode(_generatedRoot, "SmoothBankNegative");
        _gorgeWallPositive = EnsureMeshNode(_generatedRoot, "GorgeWallPositive");
        _gorgeWallNegative = EnsureMeshNode(_generatedRoot, "GorgeWallNegative");

        _collisionBody = EnsureChildNode<StaticBody3D>(_generatedRoot, "Collision");
        _terrainPositiveCollision = EnsureCollisionShapeNode(_collisionBody, "TerrainPositiveCollision");
        _terrainNegativeCollision = EnsureCollisionShapeNode(_collisionBody, "TerrainNegativeCollision");
        _smoothPositiveCollision = EnsureCollisionShapeNode(_collisionBody, "SmoothBankPositiveCollision");
        _smoothNegativeCollision = EnsureCollisionShapeNode(_collisionBody, "SmoothBankNegativeCollision");
        _gorgePositiveCollision = EnsureCollisionShapeNode(_collisionBody, "GorgeWallPositiveCollision");
        _gorgeNegativeCollision = EnsureCollisionShapeNode(_collisionBody, "GorgeWallNegativeCollision");
        _riverBlockerCollision = EnsureCollisionShapeNode(_collisionBody, "RiverBlockerCollision");
    }

    private void RebuildGeometry()
    {
        float length = Mathf.Max(2.0f, ChannelLength);
        float waterWidth = Mathf.Max(0.5f, WaterWidth);
        float waterHalf = waterWidth * 0.5f;
        float terrainPositiveWidth = Mathf.Max(0.0f, TerrainPositiveWidth);
        float terrainNegativeWidth = Mathf.Max(0.0f, TerrainNegativeWidth);
        float depth = Mathf.Max(0.05f, ChannelDepth);
        float waterY = WaterSurfaceY;
        float bedThickness = Mathf.Max(0.01f, BedThickness);
        float bedY = waterY - depth;

        float smoothRun = Mathf.Max(0.1f, SmoothBankRun);
        float smoothOuterHalf = waterHalf + smoothRun;
        float smoothRise = Mathf.Max(0.0f, -waterY);
        float smoothSlopeLength = Mathf.Sqrt((smoothRun * smoothRun) + (smoothRise * smoothRise));
        float smoothAngle = smoothSlopeLength > 0.001f
            ? Mathf.Atan2(smoothRise, smoothRun)
            : 0.0f;

        float wallThickness = Mathf.Max(0.05f, GorgeWallThickness);
        float wallHeight = Mathf.Max(0.1f, GorgeWallHeight);
        float gorgeOuterHalf = waterHalf + wallThickness;

        bool smoothStyle = BankStyle == RiverBankStyle.SmoothBanks;
        float outerHalf = smoothStyle ? smoothOuterHalf : gorgeOuterHalf;

        ConfigurePlaneMesh(_terrainPositive, length, terrainPositiveWidth, TerrainMaterial);
        ConfigurePlaneMesh(_terrainNegative, length, terrainNegativeWidth, TerrainMaterial);
        _terrainPositive.Visible = terrainPositiveWidth > 0.0f;
        _terrainNegative.Visible = terrainNegativeWidth > 0.0f;
        _terrainPositive.Position = new Vector3(0.0f, 0.0f, outerHalf + (terrainPositiveWidth * 0.5f));
        _terrainNegative.Position = new Vector3(0.0f, 0.0f, -outerHalf - (terrainNegativeWidth * 0.5f));

        ConfigurePlaneMesh(_waterSurface, length, waterWidth, WaterMaterial);
        _waterSurface.Position = new Vector3(0.0f, waterY, 0.0f);

        float bedWidth = smoothStyle
            ? waterWidth + (smoothRun * 2.0f)
            : waterWidth + (wallThickness * 2.0f);
        ConfigureBoxMesh(_riverBed, new Vector3(length, bedThickness, bedWidth), BedMaterial);
        _riverBed.Position = new Vector3(0.0f, bedY - (bedThickness * 0.5f), 0.0f);
        _riverBed.Rotation = Vector3.Zero;

        _smoothBankPositive.Visible = smoothStyle;
        _smoothBankNegative.Visible = smoothStyle;
        if (smoothStyle)
        {
            Vector3 smoothSize = new Vector3(length, Mathf.Max(0.01f, SmoothBankThickness), smoothSlopeLength);
            ConfigureBoxMesh(_smoothBankPositive, smoothSize, SmoothBankMaterial ?? TerrainMaterial);
            ConfigureBoxMesh(_smoothBankNegative, smoothSize, SmoothBankMaterial ?? TerrainMaterial);

            float midY = (waterY + 0.0f) * 0.5f;
            _smoothBankPositive.Position = new Vector3(0.0f, midY, waterHalf + (smoothRun * 0.5f));
            _smoothBankNegative.Position = new Vector3(0.0f, midY, -waterHalf - (smoothRun * 0.5f));
            _smoothBankPositive.Rotation = new Vector3(-smoothAngle, 0.0f, 0.0f);
            _smoothBankNegative.Rotation = new Vector3(smoothAngle, 0.0f, 0.0f);
        }

        _gorgeWallPositive.Visible = !smoothStyle;
        _gorgeWallNegative.Visible = !smoothStyle;
        if (!smoothStyle)
        {
            Vector3 wallSize = new Vector3(length, wallHeight, wallThickness);
            ConfigureBoxMesh(_gorgeWallPositive, wallSize, GorgeWallMaterial ?? TerrainMaterial);
            ConfigureBoxMesh(_gorgeWallNegative, wallSize, GorgeWallMaterial ?? TerrainMaterial);

            float wallY = bedY + (wallHeight * 0.5f);
            _gorgeWallPositive.Position = new Vector3(0.0f, wallY, waterHalf + (wallThickness * 0.5f));
            _gorgeWallNegative.Position = new Vector3(0.0f, wallY, -waterHalf - (wallThickness * 0.5f));
            _gorgeWallPositive.Rotation = Vector3.Zero;
            _gorgeWallNegative.Rotation = Vector3.Zero;
        }

        RebuildCollision(
            length,
            terrainPositiveWidth,
            terrainNegativeWidth,
            waterWidth,
            smoothStyle,
            smoothRun,
            smoothSlopeLength,
            smoothAngle,
            wallThickness,
            wallHeight,
            bedY,
            waterY);
    }

    private void RebuildCollision(
        float length,
        float terrainPositiveWidth,
        float terrainNegativeWidth,
        float waterWidth,
        bool smoothStyle,
        float smoothRun,
        float smoothSlopeLength,
        float smoothAngle,
        float wallThickness,
        float wallHeight,
        float bedY,
        float waterY)
    {
        bool enabled = GenerateCollision;
        _collisionBody.Visible = false;
        _terrainPositiveCollision.Disabled = !enabled || terrainPositiveWidth <= 0.0f;
        _terrainNegativeCollision.Disabled = !enabled || terrainNegativeWidth <= 0.0f;
        _smoothPositiveCollision.Disabled = !enabled || !smoothStyle;
        _smoothNegativeCollision.Disabled = !enabled || !smoothStyle;
        _gorgePositiveCollision.Disabled = !enabled || smoothStyle;
        _gorgeNegativeCollision.Disabled = !enabled || smoothStyle;
        _riverBlockerCollision.Disabled = !enabled || !BlockRiverCrossing;

        if (!enabled)
        {
            return;
        }

        float waterHalf = waterWidth * 0.5f;
        float outerHalf = smoothStyle ? (waterHalf + smoothRun) : (waterHalf + wallThickness);
        float floorThickness = Mathf.Max(0.05f, TerrainCollisionThickness);

        if (terrainPositiveWidth > 0.0f)
        {
            ConfigureCollisionBox(
                _terrainPositiveCollision,
                new Vector3(length, floorThickness, terrainPositiveWidth),
                new Vector3(0.0f, -floorThickness * 0.5f, outerHalf + (terrainPositiveWidth * 0.5f)),
                Vector3.Zero);
        }

        if (terrainNegativeWidth > 0.0f)
        {
            ConfigureCollisionBox(
                _terrainNegativeCollision,
                new Vector3(length, floorThickness, terrainNegativeWidth),
                new Vector3(0.0f, -floorThickness * 0.5f, -outerHalf - (terrainNegativeWidth * 0.5f)),
                Vector3.Zero);
        }

        if (smoothStyle)
        {
            float bankThickness = Mathf.Max(0.05f, SmoothBankThickness);
            float bankMidY = (waterY + 0.0f) * 0.5f;
            ConfigureCollisionBox(
                _smoothPositiveCollision,
                new Vector3(length, bankThickness, smoothSlopeLength),
                new Vector3(0.0f, bankMidY, waterHalf + (smoothRun * 0.5f)),
                new Vector3(-smoothAngle, 0.0f, 0.0f));
            ConfigureCollisionBox(
                _smoothNegativeCollision,
                new Vector3(length, bankThickness, smoothSlopeLength),
                new Vector3(0.0f, bankMidY, -waterHalf - (smoothRun * 0.5f)),
                new Vector3(smoothAngle, 0.0f, 0.0f));
        }
        else
        {
            ConfigureCollisionBox(
                _gorgePositiveCollision,
                new Vector3(length, wallHeight, wallThickness),
                new Vector3(0.0f, bedY + (wallHeight * 0.5f), waterHalf + (wallThickness * 0.5f)),
                Vector3.Zero);
            ConfigureCollisionBox(
                _gorgeNegativeCollision,
                new Vector3(length, wallHeight, wallThickness),
                new Vector3(0.0f, bedY + (wallHeight * 0.5f), -waterHalf - (wallThickness * 0.5f)),
                Vector3.Zero);
        }

        if (BlockRiverCrossing)
        {
            float blockerHeight = Mathf.Max(0.1f, RiverBlockerHeight);
            ConfigureCollisionBox(
                _riverBlockerCollision,
                new Vector3(length, blockerHeight, waterWidth),
                new Vector3(0.0f, waterY + (blockerHeight * 0.5f), 0.0f),
                Vector3.Zero);
        }
    }

    private static void ConfigurePlaneMesh(MeshInstance3D node, float length, float width, Material material)
    {
        var mesh = node.Mesh as PlaneMesh ?? new PlaneMesh();
        mesh.Size = new Vector2(length, width);
        node.Mesh = mesh;
        node.MaterialOverride = material;
        node.Rotation = Vector3.Zero;
    }

    private static void ConfigureBoxMesh(MeshInstance3D node, Vector3 size, Material material)
    {
        var mesh = node.Mesh as BoxMesh ?? new BoxMesh();
        mesh.Size = size;
        node.Mesh = mesh;
        node.MaterialOverride = material;
    }

    private static void ConfigureCollisionBox(CollisionShape3D shapeNode, Vector3 size, Vector3 position, Vector3 rotation)
    {
        if (shapeNode.Shape is not BoxShape3D box)
        {
            box = new BoxShape3D();
            shapeNode.Shape = box;
        }

        box.Size = size;
        shapeNode.Position = position;
        shapeNode.Rotation = rotation;
        shapeNode.Disabled = false;
    }

    private static MeshInstance3D EnsureMeshNode(Node parent, string name)
    {
        var node = parent.GetNodeOrNull<MeshInstance3D>(name);
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            return node;
        }

        node = new MeshInstance3D { Name = name };
        parent.AddChild(node);
        if (Engine.IsEditorHint())
        {
            node.Owner = parent.Owner;
        }

        return node;
    }

    private static CollisionShape3D EnsureCollisionShapeNode(Node parent, string name)
    {
        var node = parent.GetNodeOrNull<CollisionShape3D>(name);
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            return node;
        }

        node = new CollisionShape3D { Name = name };
        parent.AddChild(node);
        if (Engine.IsEditorHint())
        {
            node.Owner = parent.Owner;
        }

        return node;
    }

    private static T EnsureChildNode<T>(Node parent, string name) where T : Node, new()
    {
        var node = parent.GetNodeOrNull<T>(name);
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            return node;
        }

        node = new T { Name = name };
        parent.AddChild(node);
        if (Engine.IsEditorHint())
        {
            node.Owner = parent.Owner;
        }

        return node;
    }
}
