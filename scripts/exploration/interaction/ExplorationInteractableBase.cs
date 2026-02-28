using Godot;

/// <summary>
/// Base node for exploration interactables with condition/effect pipelines and one-shot persistence.
/// </summary>
[GlobalClass]
public partial class ExplorationInteractableBase : Node3D, IExplorationInteractable
{
    [Export]
    public string InteractionName { get; private set; } = "Interact";

    [Export]
    public InteractionTriggerMode TriggerMode { get; private set; } = InteractionTriggerMode.Action;

    [Export]
    public Godot.Collections.Array<InteractionCondition> Conditions { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<InteractionEffect> Effects { get; private set; } = new();

    [Export]
    public bool OneShot { get; private set; } = false;

    [Export]
    public string PersistentStateKey { get; private set; } = string.Empty;

    [Export]
    public bool HideWhenConsumed { get; private set; } = false;

    [Export]
    public bool DisableWhenConsumed { get; private set; } = true;

    public Node3D InteractionOrigin => this;
    public bool IsConsumed => _isConsumed;

    private bool _isConsumed;

    public override void _Ready()
    {
        AddToGroup(ExplorationGroups.Interactables);
        RefreshConsumedState();
    }

    public virtual bool CanInteract(ExplorationInteractionContext context, out string reason)
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

    public virtual void Interact(ExplorationInteractionContext context)
    {
        if (!CanInteract(context, out _))
        {
            return;
        }

        OnBeforeEffects(context);
        ExecuteEffects(context);
        OnAfterEffects(context);

        if (OneShot)
        {
            SetConsumed(true, context?.MapController);
        }
    }

    protected virtual void OnBeforeEffects(ExplorationInteractionContext context)
    {
    }

    protected virtual void OnAfterEffects(ExplorationInteractionContext context)
    {
    }

    protected virtual void OnConsumedStateChanged(bool consumed)
    {
    }

    protected void SetConsumed(bool consumed, ExplorationMapController mapController = null)
    {
        if (_isConsumed == consumed) return;

        _isConsumed = consumed;
        if (OneShot)
        {
            var controller = mapController ?? ResolveMapController();
            if (controller != null)
            {
                controller.SetMapFlag(GetResolvedStateKey(), _isConsumed);
            }
        }

        ApplyConsumedState();
    }

    protected string GetResolvedStateKey()
    {
        if (!string.IsNullOrWhiteSpace(PersistentStateKey))
        {
            return PersistentStateKey;
        }

        return $"interactable::{GetPath()}";
    }

    private void RefreshConsumedState()
    {
        if (!OneShot)
        {
            _isConsumed = false;
            ApplyConsumedState();
            return;
        }

        var controller = ResolveMapController();
        _isConsumed = controller?.GetMapFlag(GetResolvedStateKey()) ?? false;
        ApplyConsumedState();
    }

    private void ExecuteEffects(ExplorationInteractionContext context)
    {
        if (Effects == null) return;
        foreach (var effect in Effects)
        {
            effect?.Execute(context, this);
        }
    }

    private void ApplyConsumedState()
    {
        if (HideWhenConsumed)
        {
            Visible = !_isConsumed;
        }

        if (DisableWhenConsumed)
        {
            ProcessMode = _isConsumed ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
        }

        OnConsumedStateChanged(_isConsumed);
    }

    protected ExplorationMapController ResolveMapController()
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
