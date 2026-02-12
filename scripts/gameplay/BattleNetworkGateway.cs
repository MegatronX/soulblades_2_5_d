using Godot;
using System.Collections.Generic;

public sealed class BattleNetworkGateway
{
    private readonly Node _root;
    private readonly System.Func<TurnManager.TurnData> _getActiveTurn;

    public BattleNetworkGateway(Node root, System.Func<TurnManager.TurnData> getActiveTurn)
    {
        _root = root;
        _getActiveTurn = getActiveTurn;
    }

    public bool TryBuildCommitRequest(string actionPath, string itemPath, string[] targetPaths, out TurnManager.TurnData currentTurn, out ActionData action, out ItemData item, out List<Node> targets)
    {
        currentTurn = null;
        action = null;
        item = null;
        targets = new List<Node>();

        var multiplayer = _root.Multiplayer;
        if (multiplayer == null || !multiplayer.IsServer()) return false;

        currentTurn = _getActiveTurn();
        if (currentTurn == null) return false;

        var senderId = multiplayer.GetRemoteSenderId();
        if (senderId == 0) senderId = 1;
        if (currentTurn.Combatant.GetMultiplayerAuthority() != senderId)
        {
            GD.PrintErr($"Player {senderId} tried to act, but it is {currentTurn.Combatant.Name}'s turn (Owner: {currentTurn.Combatant.GetMultiplayerAuthority()})");
            return false;
        }

        action = GD.Load<ActionData>(actionPath);
        if (action == null)
        {
            GD.PrintErr($"Server could not load action from path: {actionPath}");
            return false;
        }

        if (!string.IsNullOrEmpty(itemPath))
        {
            item = GD.Load<ItemData>(itemPath);
            if (item == null)
            {
                GD.PrintErr($"Server could not load item from path: {itemPath}");
                return false;
            }
        }

        if (targetPaths != null)
        {
            foreach (var path in targetPaths)
            {
                var node = _root.GetNodeOrNull(path);
                if (node != null) targets.Add(node);
            }
        }

        return true;
    }
}
