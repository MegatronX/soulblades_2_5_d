using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles action-button interactions for nearby exploration interactables.
/// </summary>
[GlobalClass]
public partial class ExplorationInteractor : Node
{
    public const string DefaultName = "ExplorationInteractor";

    [Export]
    public string InteractAction { get; private set; } = "confirm";

    [Export(PropertyHint.Range, "0.5,10,0.1")]
    public float InteractionRange { get; private set; } = 2.2f;

    [Export]
    public bool LogFailedInteractions { get; private set; } = true;

    private Node3D _actor;
    private ExplorationMapController _mapController;

    public override void _Ready()
    {
        _actor = GetParentOrNull<Node3D>();
        _mapController = ResolveMapController();
    }

    public override void _Process(double delta)
    {
        if (!Input.IsActionJustPressed(InteractAction)) return;
        TryInteractNearest();
    }

    public bool TryInteractNearest()
    {
        if (_actor == null || !GodotObject.IsInstanceValid(_actor)) return false;
        _mapController ??= ResolveMapController();
        if (_mapController == null) return false;

        var context = BuildContext();
        if (context == null) return false;

        var candidates = GetTree()
            .GetNodesInGroup(ExplorationGroups.Interactables)
            .OfType<Node>()
            .Select(node => node as IExplorationInteractable)
            .Where(i => i != null && i.TriggerMode == InteractionTriggerMode.Action && !i.IsConsumed)
            .OrderBy(i => i.InteractionOrigin.GlobalPosition.DistanceTo(_actor.GlobalPosition))
            .ToList();

        string firstFailure = string.Empty;
        foreach (var candidate in candidates)
        {
            float distance = candidate.InteractionOrigin.GlobalPosition.DistanceTo(_actor.GlobalPosition);
            if (distance > InteractionRange) continue;

            if (!candidate.CanInteract(context, out string reason))
            {
                if (string.IsNullOrWhiteSpace(firstFailure))
                {
                    firstFailure = reason;
                }
                continue;
            }

            candidate.Interact(context);
            return true;
        }

        if (LogFailedInteractions && !string.IsNullOrWhiteSpace(firstFailure))
        {
            GD.Print($"[Interaction] Blocked: {firstFailure}");
        }

        return false;
    }

    private ExplorationInteractionContext BuildContext()
    {
        var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
        var inventory = GetNodeOrNull<InventoryManager>(InventoryManager.Path);
        return new ExplorationInteractionContext(_mapController, _actor, inventory, gameManager);
    }

    private ExplorationMapController ResolveMapController()
    {
        Node cursor = this;
        while (cursor != null)
        {
            if (cursor is ExplorationMapController controller)
            {
                return controller;
            }
            cursor = cursor.GetParent();
        }

        return GetTree()?.CurrentScene as ExplorationMapController;
    }
}
