using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class BattleTurnFlow
{
    private readonly TurnManager _turnManager;
    private readonly ActionDirector _actionDirector;
    private readonly BattleCamera _battleCamera;
    private readonly GlobalEventBus _eventBus;

    public BattleTurnFlow(TurnManager turnManager, ActionDirector actionDirector, BattleCamera battleCamera, GlobalEventBus eventBus)
    {
        _turnManager = turnManager;
        _actionDirector = actionDirector;
        _battleCamera = battleCamera;
        _eventBus = eventBus;
    }

    public TurnManager.TurnData ActiveTurn { get; private set; }

    public event System.Action<TurnManager.TurnData> TurnStarted;

    public async Task CommitAction(TurnManager.TurnData actor, ActionData action, List<Node> targets)
    {
        if (actor == null || action == null) return;

        GD.Print($"'{actor.Combatant.Name}' performs action '{action.CommandName}'.");

        var context = new ActionContext(action, actor.Combatant, targets);
        await _actionDirector.ProcessAction(context);

        _turnManager.CommitTurn(actor, action.TickCost, _actionDirector);

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
