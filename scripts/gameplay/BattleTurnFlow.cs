using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class BattleTurnFlow
{
    private readonly TurnManager _turnManager;
    private readonly ActionDirector _actionDirector;
    private readonly BattleCamera _battleCamera;
    private readonly GlobalEventBus _eventBus;
    private readonly OverflowSystem _overflowSystem;

    public BattleTurnFlow(TurnManager turnManager, ActionDirector actionDirector, BattleCamera battleCamera, GlobalEventBus eventBus, OverflowSystem overflowSystem = null)
    {
        _turnManager = turnManager;
        _actionDirector = actionDirector;
        _battleCamera = battleCamera;
        _eventBus = eventBus;
        _overflowSystem = overflowSystem;
    }

    public TurnManager.TurnData ActiveTurn { get; private set; }

    public event System.Action<TurnManager.TurnData> TurnStarted;

    public async Task CommitAction(TurnManager.TurnData actor, ActionData action, List<Node> targets, ItemData sourceItem = null)
    {
        if (actor == null || action == null) return;

        GD.Print($"'{actor.Combatant.Name}' performs action '{action.CommandName}'.");

        var context = new ActionContext(action, actor.Combatant, targets, sourceItem);
        await _actionDirector.ProcessAction(context);

        float resolvedTickCost = action.TickCost + context.TickCostAdjustment;
        resolvedTickCost = Mathf.Max(-TurnManager.TickThreshold + 1f, resolvedTickCost);

        _turnManager.CommitTurn(actor, resolvedTickCost, _actionDirector);
        _overflowSystem?.NotifyTurnCommitted(actor.Combatant);

        _eventBus?.EmitSignal(GlobalEventBus.SignalName.TurnCommitted);

        ProcessNextTurn();
    }

    public bool ProcessNextTurn()
    {
        var nextTurn = _turnManager.GetNextTurn();
        if (nextTurn == null)
        {
            GD.PrintErr("Battle ended because no combatants could take a turn.");
            ActiveTurn = null;
            return false;
        }

        if (nextTurn.IsBlocked)
        {
            GD.Print($"{nextTurn.Combatant.Name} is stopped! Skipping turn.");
            _turnManager.CommitTurn(nextTurn, 0, _actionDirector);
            return true;
        }

        ActiveTurn = nextTurn;
        TurnStarted?.Invoke(nextTurn);

        if (_battleCamera != null && nextTurn.Combatant is Node3D combatant3D)
        {
            _battleCamera.FocusOnTarget(combatant3D);
        }

        return true;
    }
}
