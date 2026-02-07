using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class BattleRoster
{
    private readonly Node _playerTeamContainer;
    private readonly Node _enemyTeamContainer;
    private readonly Node _allyTeamContainer;
    private readonly TurnManager _turnManager;
    private readonly ActionDirector _actionDirector;

    public BattleRoster(Node playerTeamContainer, Node enemyTeamContainer, Node allyTeamContainer, TurnManager turnManager, ActionDirector actionDirector)
    {
        _playerTeamContainer = playerTeamContainer;
        _enemyTeamContainer = enemyTeamContainer;
        _allyTeamContainer = allyTeamContainer;
        _turnManager = turnManager;
        _actionDirector = actionDirector;
    }

    public event System.Action<Node> CombatantDefeated;

    public void RegisterCombatants(IEnumerable<Node> combatants, float initialCounter)
    {
        foreach (var combatant in combatants)
        {
            EnsureBattleUnit(combatant);
            _turnManager.AddCombatant(combatant, initialCounter);
            ConnectCombatantSignals(combatant);
        }
    }

    public void RegisterCombatants(IEnumerable<Node> combatants, System.Func<float> initialCounterProvider)
    {
        foreach (var combatant in combatants)
        {
            EnsureBattleUnit(combatant);
            _turnManager.AddCombatant(combatant, initialCounterProvider());
            ConnectCombatantSignals(combatant);
        }
    }

    public void SummonCombatant(PackedScene characterScene, bool isPlayerSide)
    {
        if (characterScene == null) return;

        var instance = characterScene.Instantiate();
        Node container = isPlayerSide ? _allyTeamContainer : _enemyTeamContainer;

        container.AddChild(instance);

        EnsureBattleUnit(instance);
        _actionDirector.RegisterCombatant(instance);
        ConnectCombatantSignals(instance);
        _turnManager.AddCombatant(instance, 0);
    }

    public void HandleCombatantDefeated(Node combatant)
    {
        var battleUnit = combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName);
        bool persist = battleUnit?.PersistAfterDeath ?? false;

        if (combatant is CollisionObject3D col)
        {
            col.CollisionLayer = 0;
        }

        if (!persist)
        {
            var turnData = _turnManager.GetCombatants().FirstOrDefault(t => t.Combatant == combatant);
            if (turnData != null)
            {
                _turnManager.RemoveCombatant(turnData);
            }
            _actionDirector.RemoveCombatant(combatant);
            combatant.QueueFree();
        }

    }

    public void ReviveCombatant(Node combatant, int healAmount)
    {
        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null || stats.CurrentHP > 0) return;

        bool isPlayer = IsPlayerSide(combatant);
        var battleUnit = combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName);
        bool persist = battleUnit?.PersistAfterDeath ?? false;

        if (!isPlayer && !persist)
        {
            GD.Print($"Revival failed: {combatant.Name} is permanently defeated.");
            return;
        }

        stats.ModifyCurrentHP(healAmount);

        if (combatant is CollisionObject3D col) col.CollisionLayer = 1;

        var turnData = _turnManager.GetCombatants().FirstOrDefault(t => t.Combatant == combatant);
        if (turnData == null) _turnManager.AddCombatant(combatant, 0);
    }

    public bool IsCombatantAlive(Node combatant)
    {
        var battleUnit = combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName);
        if (battleUnit != null) return !battleUnit.IsDead;

        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        return stats != null && stats.CurrentHP > 0;
    }

    public IEnumerable<Node> GetOpponents(Node character)
    {
        if (IsPlayerSide(character))
        {
            return _enemyTeamContainer.GetChildren().Cast<Node>();
        }
        return _playerTeamContainer.GetChildren().Cast<Node>().Concat(_allyTeamContainer.GetChildren().Cast<Node>());
    }

    public IEnumerable<Node> GetAllies(Node character)
    {
        if (IsPlayerSide(character))
        {
            return _playerTeamContainer.GetChildren().Cast<Node>().Concat(_allyTeamContainer.GetChildren().Cast<Node>());
        }
        return _enemyTeamContainer.GetChildren().Cast<Node>();
    }

    public bool IsPlayerSide(Node character)
    {
        var parent = character.GetParent();
        return parent == _playerTeamContainer || parent == _allyTeamContainer;
    }

    private void EnsureBattleUnit(Node combatant)
    {
        if (combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName) == null)
        {
            var unit = new BattleUnit();
            unit.Name = BattleUnit.NodeName;
            combatant.AddChild(unit);
        }
    }

    private void ConnectCombatantSignals(Node combatant)
    {
        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats != null)
        {
            stats.HealthDepleted += () => OnCombatantHealthDepleted(combatant);
        }
    }

    private void OnCombatantHealthDepleted(Node combatant)
    {
        CombatantDefeated?.Invoke(combatant);
    }
}
