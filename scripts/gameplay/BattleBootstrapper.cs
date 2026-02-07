using Godot;
using System.Collections.Generic;

public sealed class BattleBootstrapper
{
    private readonly Node _playerTeamContainer;
    private readonly Node _enemyTeamContainer;
    private readonly Node _allyTeamContainer;
    private readonly BattlePlacementSettings _placementSettings;

    public BattleBootstrapper(Node playerTeamContainer, Node enemyTeamContainer, Node allyTeamContainer, BattlePlacementSettings placementSettings)
    {
        _playerTeamContainer = playerTeamContainer;
        _enemyTeamContainer = enemyTeamContainer;
        _allyTeamContainer = allyTeamContainer;
        _placementSettings = placementSettings ?? new BattlePlacementSettings();
    }

    public BattleContext BuildContextFromGameManager(GameManager gameManager)
    {
        var context = new BattleContext(gameManager.PendingBattleConfig);

        SpawnEnemyParties(context, gameManager.PendingBattleConfig?.EnemyParties);
        SpawnAllyParty(context, gameManager.PendingBattleConfig?.AllyParty);
        MovePlayerParty(context, gameManager.PlayerParty);

        _placementSettings.ApplyFormationPositions(
            context.PlayerParty,
            context.EnemyCombatants,
            context.AllyCombatants,
            context.Config.Formation
        );

        ApplyEnvironmentProfile(context.Config.EnvironmentProfile);
        return context;
    }

    private void SpawnEnemyParties(BattleContext context, Godot.Collections.Array<Godot.Collections.Array<PackedScene>> parties)
    {
        if (parties == null) return;

        foreach (var enemyParty in parties)
        {
            foreach (var enemyScene in enemyParty)
            {
                var enemyInstance = enemyScene.Instantiate();
                _enemyTeamContainer.AddChild(enemyInstance);
                context.EnemyCombatants.Add(enemyInstance);
            }
        }
    }

    private void SpawnAllyParty(BattleContext context, Godot.Collections.Array<PackedScene> allyParty)
    {
        if (allyParty == null) return;

        foreach (var allyScene in allyParty)
        {
            var allyInstance = allyScene.Instantiate();
            _allyTeamContainer.AddChild(allyInstance);
            context.AllyCombatants.Add(allyInstance);
        }
    }

    private void MovePlayerParty(BattleContext context, List<Node> playerParty)
    {
        foreach (var player in playerParty)
        {
            player.GetParent()?.RemoveChild(player);
            _playerTeamContainer.AddChild(player);
            context.PlayerParty.Add(player);
        }
    }

    private void ApplyEnvironmentProfile(BattleEnvironmentProfile profile)
    {
        if (profile == null) return;

        var sceneRoot = _playerTeamContainer.GetTree().CurrentScene;
        if (sceneRoot == null) return;

        BattleArenaGenerator arenaGenerator = null;
        foreach (var child in sceneRoot.GetChildren())
        {
            if (child is BattleArenaGenerator gen)
            {
                arenaGenerator = gen;
                break;
            }
        }

        arenaGenerator?.ApplyProfile(profile);
    }
}
