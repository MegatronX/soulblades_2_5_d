using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Path-authored river channel for complex HD-2D exploration scenes.
/// Draw the path by editing Curve points on this Path3D in editor.
/// </summary>
[Tool]
[GlobalClass]
public partial class RiverPathChannel : Path3D
{
    public enum RiverCrossSectionStyle
    {
        SmoothBanks = 0,
        RockyGorge = 1
    }

    public enum RiverFlowMode
    {
        AlongPathForward = 0,
        AlongPathBackward = 1,
        CustomVector = 2
    }

    [ExportGroup("Authoring")]
    [Export]
    public bool AutoRebuildInEditor { get; set; } = true;

    [Export]
    public bool CreateDefaultCurveIfMissing { get; set; } = true;

    [Export(PropertyHint.Range, "0.1,8,0.05,suffix:m")]
    public float SampleSpacing { get; set; } = 0.8f;

    [ExportGroup("Cross Section")]
    [Export]
    public RiverCrossSectionStyle CrossSectionStyle { get; set; } = RiverCrossSectionStyle.RockyGorge;

    [Export(PropertyHint.Range, "-8,8,0.01,suffix:m")]
    public float WaterSurfaceYOffset { get; set; } = -0.25f;

    [Export(PropertyHint.Range, "0.5,40,0.05,suffix:m")]
    public float WaterWidth { get; set; } = 8.0f;

    [Export(PropertyHint.Range, "0.0,20,0.05,suffix:m")]
    public float SmoothBankRun { get; set; } = 2.0f;

    [Export(PropertyHint.Range, "0.05,20,0.05,suffix:m")]
    public float BedDepth { get; set; } = 1.35f;

    [Export(PropertyHint.Range, "0.0,20,0.05,suffix:m")]
    public float BedWidthExtra { get; set; } = 1.1f;

    [Export(PropertyHint.Range, "0.0,8,0.01,suffix:m")]
    public float GorgeRimHeight { get; set; } = 1.05f;

    [Export(PropertyHint.Range, "0.0,10,0.05,suffix:m")]
    public float GorgeLedgeRun { get; set; } = 1.2f;

    [ExportGroup("Flow")]
    [Export]
    public RiverFlowMode FlowMode { get; set; } = RiverFlowMode.AlongPathForward;

    [Export]
    public Vector2 CustomFlowDirection { get; set; } = new Vector2(0.0f, -1.0f);

    [Export(PropertyHint.Range, "0.01,8,0.01")]
    public float WaterUvLengthScale { get; set; } = 0.08f;

    [Export(PropertyHint.Range, "0.01,8,0.01")]
    public float WaterUvWidthScale { get; set; } = 1.0f;

    [Export(PropertyHint.Range, "0.01,8,0.01")]
    public float TerrainUvLengthScale { get; set; } = 0.12f;

    [ExportGroup("Boat Support")]
    [Export]
    public bool GenerateBoatPath { get; set; } = true;

    [Export(PropertyHint.Range, "-2,4,0.01,suffix:m")]
    public float BoatPathYOffset { get; set; } = 0.05f;

    [Export]
    public bool EnableBoatPreview { get; set; } = false;

    [Export(PropertyHint.Range, "0.01,20,0.01,suffix:m/s")]
    public float BoatPreviewSpeed { get; set; } = 1.6f;

    [Export(PropertyHint.Range, "0.1,8,0.01")]
    public float BoatPreviewScale { get; set; } = 0.75f;

    [ExportGroup("Obstacle Anchors")]
    [Export]
    public bool GenerateObstacleAnchors { get; set; } = true;

    [Export(PropertyHint.Range, "0.5,40,0.1,suffix:m")]
    public float ObstacleAnchorInterval { get; set; } = 5.5f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float ObstacleEdgeBias { get; set; } = 0.74f;

    [Export(PropertyHint.Range, "-2,4,0.01,suffix:m")]
    public float ObstacleHeightOffset { get; set; } = 0.03f;

    [Export]
    public bool AlternateObstacleSides { get; set; } = true;

    [Export]
    public int ObstacleSeed { get; set; } = 117;

    [Export(PropertyHint.Range, "1,128,1")]
    public int MaxObstacleAnchors { get; set; } = 32;

    [ExportGroup("Materials")]
    [Export]
    public Material WaterMaterial { get; set; }

    [Export]
    public Material BedMaterial { get; set; }

    [Export]
    public Material TerrainMaterial { get; set; }

    [Export]
    public Material GorgeWallMaterial { get; set; }

    private sealed class Frame
    {
        public Vector3 Center;
        public Vector3 Right;
        public Vector3 Up;
        public float Distance;
    }

    private Node3D _generatedRoot;
    private MeshInstance3D _waterMesh;
    private MeshInstance3D _bedMesh;
    private MeshInstance3D _leftTerrainMesh;
    private MeshInstance3D _rightTerrainMesh;
    private MeshInstance3D _leftWallMesh;
    private MeshInstance3D _rightWallMesh;
    private Node3D _obstacleAnchorRoot;
    private Path3D _boatPath;
    private PathFollow3D _boatPreviewFollow;
    private MeshInstance3D _boatPreviewMesh;

    private int _lastConfigHash;
    private readonly RandomNumberGenerator _obstacleRng = new();

    public override void _Ready()
    {
        EnsureCurve();
        RefreshGenerated();
        SetProcess(Engine.IsEditorHint() && AutoRebuildInEditor);
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            if (!AutoRebuildInEditor) return;
            int hash = ComputeConfigHash();
            if (hash == _lastConfigHash) return;
            RefreshGenerated();
            return;
        }

        if (EnableBoatPreview && _boatPreviewFollow != null && GodotObject.IsInstanceValid(_boatPreviewFollow))
        {
            _boatPreviewFollow.Progress += Mathf.Max(0.01f, BoatPreviewSpeed) * (float)delta;
        }
    }

    public void RefreshGenerated()
    {
        EnsureCurve();
        EnsureGeneratedNodes();

        List<Frame> frames = BuildFrames();
        BuildMeshes(frames);
        UpdateFlowDirection();
        BuildObstacleAnchors(frames);
        UpdateBoatPathAndPreview();

        _lastConfigHash = ComputeConfigHash();
    }

    private void EnsureCurve()
    {
        if (Curve != null && Curve.PointCount >= 2) return;
        if (!CreateDefaultCurveIfMissing) return;

        var curve = new Curve3D();
        curve.AddPoint(new Vector3(-22f, 0f, 8f));
        curve.AddPoint(new Vector3(-8f, 0f, 4f));
        curve.AddPoint(new Vector3(8f, 0f, -3f));
        curve.AddPoint(new Vector3(23f, 0f, -10f));
        Curve = curve;
    }

    private int ComputeConfigHash()
    {
        var hash = new HashCode();
        hash.Add(AutoRebuildInEditor);
        hash.Add(SampleSpacing);
        hash.Add((int)CrossSectionStyle);
        hash.Add(WaterSurfaceYOffset);
        hash.Add(WaterWidth);
        hash.Add(SmoothBankRun);
        hash.Add(BedDepth);
        hash.Add(BedWidthExtra);
        hash.Add(GorgeRimHeight);
        hash.Add(GorgeLedgeRun);
        hash.Add((int)FlowMode);
        hash.Add(CustomFlowDirection);
        hash.Add(WaterUvLengthScale);
        hash.Add(WaterUvWidthScale);
        hash.Add(TerrainUvLengthScale);
        hash.Add(GenerateBoatPath);
        hash.Add(BoatPathYOffset);
        hash.Add(EnableBoatPreview);
        hash.Add(BoatPreviewSpeed);
        hash.Add(BoatPreviewScale);
        hash.Add(GenerateObstacleAnchors);
        hash.Add(ObstacleAnchorInterval);
        hash.Add(ObstacleEdgeBias);
        hash.Add(ObstacleHeightOffset);
        hash.Add(AlternateObstacleSides);
        hash.Add(ObstacleSeed);
        hash.Add(MaxObstacleAnchors);
        hash.Add(WaterMaterial);
        hash.Add(BedMaterial);
        hash.Add(TerrainMaterial);
        hash.Add(GorgeWallMaterial);

        if (Curve != null)
        {
            hash.Add(Curve.PointCount);
            for (int i = 0; i < Curve.PointCount; i++)
            {
                hash.Add(Curve.GetPointPosition(i));
                hash.Add(Curve.GetPointIn(i));
                hash.Add(Curve.GetPointOut(i));
                hash.Add(Curve.GetPointTilt(i));
            }
        }
        else
        {
            hash.Add(0);
        }

        return hash.ToHashCode();
    }

    private void EnsureGeneratedNodes()
    {
        _generatedRoot = EnsureChildNode<Node3D>(this, "__Generated");
        _waterMesh = EnsureMeshNode(_generatedRoot, "WaterMesh");
        _bedMesh = EnsureMeshNode(_generatedRoot, "BedMesh");
        _leftTerrainMesh = EnsureMeshNode(_generatedRoot, "LeftTerrainMesh");
        _rightTerrainMesh = EnsureMeshNode(_generatedRoot, "RightTerrainMesh");
        _leftWallMesh = EnsureMeshNode(_generatedRoot, "LeftWallMesh");
        _rightWallMesh = EnsureMeshNode(_generatedRoot, "RightWallMesh");
        _obstacleAnchorRoot = EnsureChildNode<Node3D>(_generatedRoot, "ObstacleAnchors");
        _boatPath = EnsureChildNode<Path3D>(_generatedRoot, "BoatPath");
        _boatPreviewFollow = EnsureChildNode<PathFollow3D>(_boatPath, "BoatPreviewFollow");
        _boatPreviewMesh = EnsureMeshNode(_boatPreviewFollow, "BoatPreviewMesh");
    }

    private List<Frame> BuildFrames()
    {
        var frames = new List<Frame>();
        if (Curve == null || Curve.PointCount < 2)
        {
            return frames;
        }

        float bakedLength = Mathf.Max(0.01f, Curve.GetBakedLength());
        float spacing = Mathf.Max(0.1f, SampleSpacing);
        int sampleCount = Mathf.Max(2, Mathf.CeilToInt(bakedLength / spacing) + 1);

        for (int i = 0; i < sampleCount; i++)
        {
            float dist = Mathf.Min(bakedLength, i * spacing);
            float prevDist = Mathf.Max(0.0f, dist - 0.1f);
            float nextDist = Mathf.Min(bakedLength, dist + 0.1f);

            Vector3 center = Curve.SampleBaked(dist, true);
            Vector3 prev = Curve.SampleBaked(prevDist, true);
            Vector3 next = Curve.SampleBaked(nextDist, true);

            Vector3 tangent = (next - prev).Normalized();
            if (tangent.LengthSquared() < 0.0001f)
            {
                tangent = Vector3.Forward;
            }

            Vector3 up = Vector3.Up;
            Vector3 right = tangent.Cross(up).Normalized();
            if (right.LengthSquared() < 0.0001f)
            {
                right = Vector3.Right;
            }
            up = right.Cross(tangent).Normalized();

            frames.Add(new Frame
            {
                Center = center,
                Right = right,
                Up = up,
                Distance = dist
            });
        }

        return frames;
    }

    private void BuildMeshes(List<Frame> frames)
    {
        if (frames.Count < 2)
        {
            _waterMesh.Mesh = null;
            _bedMesh.Mesh = null;
            _leftTerrainMesh.Mesh = null;
            _rightTerrainMesh.Mesh = null;
            _leftWallMesh.Mesh = null;
            _rightWallMesh.Mesh = null;
            return;
        }

        float waterHalf = Mathf.Max(0.25f, WaterWidth * 0.5f);
        float bedHalf = waterHalf + Mathf.Max(0.0f, BedWidthExtra);
        float terrainRun = Mathf.Max(0.0f, SmoothBankRun);
        float rimHeight = Mathf.Max(0.0f, GorgeRimHeight);
        float ledgeRun = Mathf.Max(0.0f, GorgeLedgeRun);
        bool rocky = CrossSectionStyle == RiverCrossSectionStyle.RockyGorge;

        var waterTool = new SurfaceTool();
        var bedTool = new SurfaceTool();
        var leftTerrainTool = new SurfaceTool();
        var rightTerrainTool = new SurfaceTool();
        var leftWallTool = new SurfaceTool();
        var rightWallTool = new SurfaceTool();
        waterTool.Begin(Mesh.PrimitiveType.Triangles);
        bedTool.Begin(Mesh.PrimitiveType.Triangles);
        leftTerrainTool.Begin(Mesh.PrimitiveType.Triangles);
        rightTerrainTool.Begin(Mesh.PrimitiveType.Triangles);
        leftWallTool.Begin(Mesh.PrimitiveType.Triangles);
        rightWallTool.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < frames.Count - 1; i++)
        {
            Frame a = frames[i];
            Frame b = frames[i + 1];

            float waterV0 = a.Distance * Mathf.Max(0.001f, WaterUvLengthScale);
            float waterV1 = b.Distance * Mathf.Max(0.001f, WaterUvLengthScale);
            float terrainV0 = a.Distance * Mathf.Max(0.001f, TerrainUvLengthScale);
            float terrainV1 = b.Distance * Mathf.Max(0.001f, TerrainUvLengthScale);

            Vector3 aWaterCenter = a.Center + (a.Up * WaterSurfaceYOffset);
            Vector3 bWaterCenter = b.Center + (b.Up * WaterSurfaceYOffset);
            Vector3 aWaterL = aWaterCenter - (a.Right * waterHalf);
            Vector3 aWaterR = aWaterCenter + (a.Right * waterHalf);
            Vector3 bWaterL = bWaterCenter - (b.Right * waterHalf);
            Vector3 bWaterR = bWaterCenter + (b.Right * waterHalf);

            AddQuad(
                waterTool,
                aWaterL, bWaterL, bWaterR, aWaterR,
                new Vector2(0f, waterV0),
                new Vector2(0f, waterV1),
                new Vector2(1f * WaterUvWidthScale, waterV1),
                new Vector2(1f * WaterUvWidthScale, waterV0),
                (a.Up + b.Up).Normalized());

            Vector3 aBedCenter = aWaterCenter + (a.Up * -Mathf.Max(0.01f, BedDepth));
            Vector3 bBedCenter = bWaterCenter + (b.Up * -Mathf.Max(0.01f, BedDepth));
            Vector3 aBedL = aBedCenter - (a.Right * bedHalf);
            Vector3 aBedR = aBedCenter + (a.Right * bedHalf);
            Vector3 bBedL = bBedCenter - (b.Right * bedHalf);
            Vector3 bBedR = bBedCenter + (b.Right * bedHalf);

            AddQuad(
                bedTool,
                aBedL, bBedL, bBedR, aBedR,
                new Vector2(0f, terrainV0),
                new Vector2(0f, terrainV1),
                new Vector2(1f, terrainV1),
                new Vector2(1f, terrainV0),
                (a.Up + b.Up).Normalized());

            if (!rocky)
            {
                Vector3 aTerrainCenter = a.Center;
                Vector3 bTerrainCenter = b.Center;
                Vector3 aBankL = aTerrainCenter - (a.Right * (waterHalf + terrainRun));
                Vector3 bBankL = bTerrainCenter - (b.Right * (waterHalf + terrainRun));
                Vector3 aBankR = aTerrainCenter + (a.Right * (waterHalf + terrainRun));
                Vector3 bBankR = bTerrainCenter + (b.Right * (waterHalf + terrainRun));

                Vector3 leftNormal = ((aWaterL - aBankL).Cross(bBankL - aBankL)).Normalized();
                if (leftNormal.LengthSquared() < 0.0001f) leftNormal = Vector3.Up;
                AddQuad(
                    leftTerrainTool,
                    aBankL, bBankL, bWaterL, aWaterL,
                    new Vector2(0f, terrainV0),
                    new Vector2(0f, terrainV1),
                    new Vector2(1f, terrainV1),
                    new Vector2(1f, terrainV0),
                    leftNormal);

                Vector3 rightNormal = ((aBankR - aWaterR).Cross(bWaterR - aWaterR)).Normalized();
                if (rightNormal.LengthSquared() < 0.0001f) rightNormal = Vector3.Up;
                AddQuad(
                    rightTerrainTool,
                    aWaterR, bWaterR, bBankR, aBankR,
                    new Vector2(0f, terrainV0),
                    new Vector2(0f, terrainV1),
                    new Vector2(1f, terrainV1),
                    new Vector2(1f, terrainV0),
                    rightNormal);
            }
            else
            {
                Vector3 aRimCenter = aWaterCenter + (a.Up * rimHeight);
                Vector3 bRimCenter = bWaterCenter + (b.Up * rimHeight);
                Vector3 aRimL = aRimCenter - (a.Right * waterHalf);
                Vector3 bRimL = bRimCenter - (b.Right * waterHalf);
                Vector3 aRimR = aRimCenter + (a.Right * waterHalf);
                Vector3 bRimR = bRimCenter + (b.Right * waterHalf);
                Vector3 aRimOuterL = aRimCenter - (a.Right * (waterHalf + ledgeRun));
                Vector3 bRimOuterL = bRimCenter - (b.Right * (waterHalf + ledgeRun));
                Vector3 aRimOuterR = aRimCenter + (a.Right * (waterHalf + ledgeRun));
                Vector3 bRimOuterR = bRimCenter + (b.Right * (waterHalf + ledgeRun));

                AddQuad(
                    leftWallTool,
                    aBedL, bBedL, bRimL, aRimL,
                    new Vector2(0f, terrainV0),
                    new Vector2(0f, terrainV1),
                    new Vector2(1f, terrainV1),
                    new Vector2(1f, terrainV0),
                    -((a.Right + b.Right).Normalized()));

                AddQuad(
                    rightWallTool,
                    aRimR, bRimR, bBedR, aBedR,
                    new Vector2(0f, terrainV0),
                    new Vector2(0f, terrainV1),
                    new Vector2(1f, terrainV1),
                    new Vector2(1f, terrainV0),
                    (a.Right + b.Right).Normalized());

                AddQuad(
                    leftTerrainTool,
                    aRimOuterL, bRimOuterL, bRimL, aRimL,
                    new Vector2(0f, terrainV0),
                    new Vector2(0f, terrainV1),
                    new Vector2(1f, terrainV1),
                    new Vector2(1f, terrainV0),
                    Vector3.Up);

                AddQuad(
                    rightTerrainTool,
                    aRimR, bRimR, bRimOuterR, aRimOuterR,
                    new Vector2(0f, terrainV0),
                    new Vector2(0f, terrainV1),
                    new Vector2(1f, terrainV1),
                    new Vector2(1f, terrainV0),
                    Vector3.Up);
            }
        }

        _waterMesh.Mesh = waterTool.Commit();
        _bedMesh.Mesh = bedTool.Commit();
        _leftTerrainMesh.Mesh = leftTerrainTool.Commit();
        _rightTerrainMesh.Mesh = rightTerrainTool.Commit();
        _leftWallMesh.Mesh = leftWallTool.Commit();
        _rightWallMesh.Mesh = rightWallTool.Commit();

        _waterMesh.MaterialOverride = WaterMaterial;
        _bedMesh.MaterialOverride = BedMaterial;
        _leftTerrainMesh.MaterialOverride = TerrainMaterial;
        _rightTerrainMesh.MaterialOverride = TerrainMaterial;
        _leftWallMesh.MaterialOverride = GorgeWallMaterial ?? TerrainMaterial;
        _rightWallMesh.MaterialOverride = GorgeWallMaterial ?? TerrainMaterial;

        _leftWallMesh.Visible = rocky;
        _rightWallMesh.Visible = rocky;
    }

    private void BuildObstacleAnchors(List<Frame> frames)
    {
        if (_obstacleAnchorRoot == null) return;
        ClearChildren(_obstacleAnchorRoot);

        if (!GenerateObstacleAnchors || frames.Count < 2)
        {
            return;
        }

        float length = frames[^1].Distance;
        float interval = Mathf.Max(0.5f, ObstacleAnchorInterval);
        int targetCount = Mathf.Min(MaxObstacleAnchors, Mathf.Max(0, Mathf.FloorToInt(length / interval)));
        if (targetCount <= 0) return;

        _obstacleRng.Seed = (ulong)Mathf.Abs(ObstacleSeed);
        float waterHalf = Mathf.Max(0.25f, WaterWidth * 0.5f);
        float lateralDistance = waterHalf * Mathf.Clamp(ObstacleEdgeBias, 0.0f, 1.0f);

        for (int i = 0; i < targetCount; i++)
        {
            float dist = Mathf.Min(length, (i + 1) * interval);
            Frame frame = SampleFrameAtDistance(frames, dist);
            float sideSign = AlternateObstacleSides
                ? ((i % 2 == 0) ? 1f : -1f)
                : (_obstacleRng.Randf() < 0.5f ? -1f : 1f);
            float jitter = _obstacleRng.RandfRange(-0.25f, 0.25f) * Mathf.Max(0.1f, lateralDistance);
            Vector3 waterCenter = frame.Center + (frame.Up * WaterSurfaceYOffset);
            Vector3 position = waterCenter
                + (frame.Right * (sideSign * (lateralDistance + jitter)))
                + (frame.Up * ObstacleHeightOffset);

            var anchor = new Marker3D
            {
                Name = $"ObstacleAnchor_{i:00}",
                Position = position
            };
            _obstacleAnchorRoot.AddChild(anchor);
        }
    }

    private void UpdateBoatPathAndPreview()
    {
        if (_boatPath == null)
        {
            return;
        }

        if (!GenerateBoatPath || Curve == null || Curve.PointCount < 2)
        {
            _boatPath.Curve = null;
            if (_boatPreviewFollow != null) _boatPreviewFollow.Visible = false;
            return;
        }

        var newCurve = new Curve3D();
        for (int i = 0; i < Curve.PointCount; i++)
        {
            Vector3 pos = Curve.GetPointPosition(i) + (Vector3.Up * BoatPathYOffset);
            Vector3 inHandle = Curve.GetPointIn(i);
            Vector3 outHandle = Curve.GetPointOut(i);
            float tilt = Curve.GetPointTilt(i);
            newCurve.AddPoint(pos, inHandle, outHandle);
            newCurve.SetPointTilt(i, tilt);
        }

        _boatPath.Curve = newCurve;
        _boatPath.Visible = true;

        if (_boatPreviewFollow == null || _boatPreviewMesh == null)
        {
            return;
        }

        _boatPreviewFollow.Visible = EnableBoatPreview;
        _boatPreviewFollow.Loop = true;

        if (EnableBoatPreview)
        {
            var mesh = _boatPreviewMesh.Mesh as BoxMesh ?? new BoxMesh();
            mesh.Size = new Vector3(1.6f, 0.35f, 0.75f) * Mathf.Max(0.05f, BoatPreviewScale);
            _boatPreviewMesh.Mesh = mesh;
            if (_boatPreviewMesh.MaterialOverride == null)
            {
                _boatPreviewMesh.MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.639f, 0.49f, 0.302f),
                    Roughness = 0.86f
                };
            }
        }
    }

    private void UpdateFlowDirection()
    {
        if (WaterMaterial is not ShaderMaterial shaderMaterial)
        {
            return;
        }

        Vector2 flowDirection = FlowMode switch
        {
            RiverFlowMode.AlongPathForward => new Vector2(0.0f, -1.0f),
            RiverFlowMode.AlongPathBackward => new Vector2(0.0f, 1.0f),
            _ => CustomFlowDirection
        };

        if (flowDirection.LengthSquared() <= 0.0001f)
        {
            flowDirection = new Vector2(0.0f, -1.0f);
        }
        else
        {
            flowDirection = flowDirection.Normalized();
        }

        shaderMaterial.SetShaderParameter("flow_direction", flowDirection);
    }

    private static void AddQuad(
        SurfaceTool tool,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector2 uvA,
        Vector2 uvB,
        Vector2 uvC,
        Vector2 uvD,
        Vector3 normal)
    {
        Vector3 n = normal.LengthSquared() > 0.0001f ? normal.Normalized() : Vector3.Up;

        AddVertex(tool, a, n, uvA);
        AddVertex(tool, b, n, uvB);
        AddVertex(tool, c, n, uvC);

        AddVertex(tool, a, n, uvA);
        AddVertex(tool, c, n, uvC);
        AddVertex(tool, d, n, uvD);
    }

    private static void AddVertex(SurfaceTool tool, Vector3 position, Vector3 normal, Vector2 uv)
    {
        tool.SetNormal(normal);
        tool.SetUV(uv);
        tool.AddVertex(position);
    }

    private static Frame SampleFrameAtDistance(List<Frame> frames, float distance)
    {
        if (frames.Count == 0)
        {
            return new Frame { Center = Vector3.Zero, Up = Vector3.Up, Right = Vector3.Right, Distance = 0f };
        }

        if (distance <= frames[0].Distance) return frames[0];
        if (distance >= frames[^1].Distance) return frames[^1];

        for (int i = 0; i < frames.Count - 1; i++)
        {
            Frame a = frames[i];
            Frame b = frames[i + 1];
            if (distance < a.Distance || distance > b.Distance) continue;

            float segment = Mathf.Max(0.0001f, b.Distance - a.Distance);
            float t = Mathf.Clamp((distance - a.Distance) / segment, 0f, 1f);

            return new Frame
            {
                Distance = distance,
                Center = a.Center.Lerp(b.Center, t),
                Right = a.Right.Lerp(b.Right, t).Normalized(),
                Up = a.Up.Lerp(b.Up, t).Normalized()
            };
        }

        return frames[^1];
    }

    private static void ClearChildren(Node parent)
    {
        if (parent == null) return;
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static MeshInstance3D EnsureMeshNode(Node parent, string name)
    {
        var node = parent.GetNodeOrNull<MeshInstance3D>(name);
        if (node != null && GodotObject.IsInstanceValid(node)) return node;

        node = new MeshInstance3D { Name = name };
        parent.AddChild(node);
        if (Engine.IsEditorHint()) node.Owner = parent.Owner;
        return node;
    }

    private static T EnsureChildNode<T>(Node parent, string name) where T : Node, new()
    {
        var node = parent.GetNodeOrNull<T>(name);
        if (node != null && GodotObject.IsInstanceValid(node)) return node;

        node = new T { Name = name };
        parent.AddChild(node);
        if (Engine.IsEditorHint()) node.Owner = parent.Owner;
        return node;
    }
}
