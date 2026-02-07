using Godot;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// A debug overlay that displays the internal state of all active AIControllers.
/// Toggle with F4.
/// </summary>
public partial class AIDebugOverlay : CanvasLayer
{
    private Control _root;
    private VBoxContainer _listContainer;
    private Dictionary<AIController, Control> _aiEntries = new();
    private Node _currentHighlight;
    private MeshInstance3D _highlightBox;

    public override void _Ready()
    {
        // Create UI programmatically to avoid needing a .tscn file for a debug tool
        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_root);

        var panel = new PanelContainer();
        // Anchor to the left side, stretching vertically from top to bottom
        panel.AnchorLeft = 0;
        panel.AnchorTop = 0;
        panel.AnchorBottom = 1;
        panel.AnchorRight = 0; // Allow width to grow based on content
        
        // Add margins
        panel.OffsetLeft = 10;
        panel.OffsetTop = 10;
        panel.OffsetBottom = -10;
        
        // Add a semi-transparent background for readability
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0, 0, 0, 0.5f);
        panel.AddThemeStyleboxOverride("panel", styleBox);
        
        _root.AddChild(panel);

        // Wrap the label in a ScrollContainer
        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        panel.AddChild(scroll);

        _listContainer = new VBoxContainer();
        _listContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _listContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_listContainer);

        AddHeader();

        _root.Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.F4)
        {
            _root.Visible = !_root.Visible;
            if (!_root.Visible) RemoveHighlight();
        }
    }

    public override void _Process(double delta)
    {
        if (!_root.Visible) return;

        // 1. Sync entries with active controllers
        // Use a copy to safely iterate
        var activeControllers = new List<AIController>(AIController.ActiveControllers);
        var currentSet = new HashSet<AIController>(activeControllers);
        var toRemove = new List<AIController>();

        // Identify removed controllers
        foreach (var kvp in _aiEntries)
        {
            if (!currentSet.Contains(kvp.Key) || !GodotObject.IsInstanceValid(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        // Remove old entries
        foreach (var ctrl in toRemove)
        {
            _aiEntries[ctrl].QueueFree();
            _aiEntries.Remove(ctrl);
            if (_currentHighlight == ctrl.GetParent()) RemoveHighlight();
        }

        // Add new entries
        foreach (var ai in activeControllers)
        {
            if (!_aiEntries.ContainsKey(ai))
            {
                var entry = CreateAIEntry(ai);
                _listContainer.AddChild(entry);
                _aiEntries[ai] = entry;
            }
        }

        // 2. Update text content
        foreach (var kvp in _aiEntries)
        {
            UpdateAIEntry(kvp.Key, kvp.Value);
        }
    }

    private void AddHeader()
    {
        var label = new Label();
        label.Text = "--- AI DEBUG (F4) ---";
        label.LabelSettings = new LabelSettings { FontColor = Colors.Yellow, OutlineSize = 2, OutlineColor = Colors.Black };
        _listContainer.AddChild(label);
    }

    private Control CreateAIEntry(AIController ai)
    {
        var panel = new PanelContainer();
        panel.MouseFilter = Control.MouseFilterEnum.Stop; // Capture mouse events
        
        var style = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.2f) };
        panel.AddThemeStyleboxOverride("panel", style);

        var label = new Label();
        label.Name = "Label";
        label.LabelSettings = new LabelSettings 
        { 
            OutlineSize = 2, 
            OutlineColor = Colors.Black,
            FontColor = Colors.White
        };
        panel.AddChild(label);

        // Hover Logic
        panel.MouseEntered += () => 
        {
            style.BgColor = new Color(0.3f, 0.3f, 0.3f, 0.6f); // Highlight UI
            OnEntryHover(ai);
        };
        panel.MouseExited += () => 
        {
            style.BgColor = new Color(0, 0, 0, 0.2f); // Reset UI
            OnEntryUnhover(ai);
        };

        return panel;
    }

    private void UpdateAIEntry(AIController ai, Control entry)
    {
        var label = entry.GetNode<Label>("Label");
        var owner = ai.GetParent();
        
        string text = $"[{owner.Name}]\n";
        text += $"  Strategy: {(ai.Strategy != null ? ai.Strategy.ResourceName : "None")}\n";
        
        if (ai.Memory.Count > 0)
        {
            text += "  Memory:\n";
            foreach (var kvp in ai.Memory)
            {
                if (kvp.Value.Obj is string s && s.Contains('\n'))
                {
                    text += $"    {kvp.Key}:\n      {s.Replace("\n", "\n      ")}\n";
                }
                else
                {
                    text += $"    {kvp.Key}: {kvp.Value}\n";
                }
            }
        }

        var threatTable = ai.Debug_GetThreatTable();
        if (threatTable.Count > 0)
        {
            text += "  Threat:\n";
            foreach (var kvp in threatTable.OrderByDescending(x => x.Value))
            {
                var target = GodotObject.InstanceFromId(kvp.Key) as Node;
                var name = target != null ? target.Name.ToString() : $"ID:{kvp.Key}";
                text += $"    {name}: {kvp.Value:F1}\n";
            }
        }
        
        var immunity = ai.Debug_GetImmunityMemory();
        if (immunity.Count > 0)
        {
            text += "  Immunity:\n";
            foreach (var kvp in immunity)
            {
                var target = GodotObject.InstanceFromId(kvp.Key) as Node;
                var name = target != null ? target.Name.ToString() : $"ID:{kvp.Key}";
                text += $"    {name}: [{string.Join(", ", kvp.Value)}]\n";
            }
        }

        label.Text = text;
    }

    private void OnEntryHover(AIController ai)
    {
        var owner = ai.GetParent();
        if (owner == null) return;

        if (_currentHighlight != owner)
        {
            RemoveHighlight();
            _currentHighlight = owner;
            AddHighlight(owner);
        }
    }

    private void OnEntryUnhover(AIController ai)
    {
        var owner = ai.GetParent();
        if (_currentHighlight == owner)
        {
            RemoveHighlight();
        }
    }

    private void AddHighlight(Node target)
    {
        if (target is Node3D node3d)
        {
            _highlightBox = new MeshInstance3D();
            
            // Estimate height based on visuals
            float height = 2.0f;
            foreach (var child in node3d.GetChildren())
            {
                if (child is GeometryInstance3D visual && visual.Visible)
                {
                    var aabb = visual.GetAabb();
                    float topY = visual.Position.Y + (aabb.End.Y * visual.Scale.Y);
                    if (topY > height) height = topY;
                }
            }

            var mesh = new BoxMesh();
            mesh.Size = new Vector3(1.5f, height, 1.5f);
            _highlightBox.Mesh = mesh;
            _highlightBox.Position = new Vector3(0, height / 2.0f, 0);

            var mat = new StandardMaterial3D();
            mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            mat.AlbedoColor = new Color(1, 1, 0, 0.3f); // Yellow transparent
            mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            _highlightBox.MaterialOverride = mat;
            
            node3d.AddChild(_highlightBox);
        }
    }

    private void RemoveHighlight()
    {
        if (_highlightBox != null && GodotObject.IsInstanceValid(_highlightBox))
        {
            _highlightBox.QueueFree();
        }
        _highlightBox = null;
        _currentHighlight = null;
    }
}
