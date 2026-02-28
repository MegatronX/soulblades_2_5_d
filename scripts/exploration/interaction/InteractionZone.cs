using Godot;

/// <summary>
/// Area-based interaction node supporting step-trigger and action-trigger modes.
/// </summary>
[GlobalClass]
public partial class InteractionZone : Area3D, IExplorationInteractable
{
    [Export]
    public string InteractionName { get; private set; } = "Interaction Zone";

    [Export]
    public InteractionTriggerMode TriggerMode { get; private set; } = InteractionTriggerMode.StepEnter;

    [Export]
    public Godot.Collections.Array<InteractionCondition> Conditions { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<InteractionEffect> Effects { get; private set; } = new();

    [Export]
    public bool OneShot { get; private set; } = false;

    [Export]
    public string PersistentStateKey { get; private set; } = string.Empty;

    [Export]
    public bool RestrictToPlayerCharacters { get; private set; } = true;

    [Export]
    public bool AllowRetriggerWhileInside { get; private set; } = false;

    public Node3D InteractionOrigin => this;
    public bool IsConsumed => _isConsumed;

    private bool _triggeredForCurrentOccupant;
    private bool _isConsumed;

    public override void _Ready()
    {
        AddToGroup(ExplorationGroups.Interactables);
        RefreshConsumedState();

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public bool CanInteract(ExplorationInteractionContext context, out string reason)
    {
        if (_isConsumed)
        {
            reason = $"{InteractionName} already used.";
            return false;
        }

        if (Conditions != null)
        {
            foreach (var condition in Conditions)
            {
                if (condition == null) continue;
                if (!condition.IsSatisfied(context, out reason))
                {
                    return false;
                }
            }
        }

        reason = string.Empty;
        return true;
    }

    public void Interact(ExplorationInteractionContext context)
    {
        if (!CanInteract(context, out _)) return;

        if (Effects != null)
        {
            foreach (var effect in Effects)
            {
                effect?.Execute(context, this);
            }
        }

        if (OneShot)
        {
            SetConsumed(true, context?.MapController);
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (TriggerMode != InteractionTriggerMode.StepEnter) return;
        if (body == null) return;
        if (RestrictToPlayerCharacters && !body.IsInGroup(GameGroups.PlayerCharacters)) return;
        if (!AllowRetriggerWhileInside && _triggeredForCurrentOccupant) return;

        var context = BuildContext(body);
        if (context == null) return;

        Interact(context);
        _triggeredForCurrentOccupant = true;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body == null) return;
        if (RestrictToPlayerCharacters && !body.IsInGroup(GameGroups.PlayerCharacters)) return;
        _triggeredForCurrentOccupant = false;
    }

    private void RefreshConsumedState()
    {
        if (!OneShot)
        {
            _isConsumed = false;
            return;
        }

        var controller = ResolveMapController();
        _isConsumed = controller?.GetMapFlag(GetResolvedStateKey()) ?? false;
    }

    private void SetConsumed(bool consumed, ExplorationMapController mapController = null)
    {
        if (_isConsumed == consumed) return;
        _isConsumed = consumed;

        if (!OneShot) return;
        var controller = mapController ?? ResolveMapController();
        controller?.SetMapFlag(GetResolvedStateKey(), _isConsumed);
    }

    private string GetResolvedStateKey()
    {
        if (!string.IsNullOrWhiteSpace(PersistentStateKey))
        {
            return PersistentStateKey;
        }

        return $"zone::{GetPath()}";
    }

    private ExplorationInteractionContext BuildContext(Node interactor)
    {
        var mapController = ResolveMapController();
        if (mapController == null) return null;

        var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
        var inventory = GetNodeOrNull<InventoryManager>(InventoryManager.Path);
        return new ExplorationInteractionContext(mapController, interactor, inventory, gameManager);
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
        return null;
    }
}
