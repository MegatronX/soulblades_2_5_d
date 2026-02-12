using Godot;
using System;
using System.ComponentModel;

public partial class BattleStarterTestScript : Node
{
    [Export]
    private EncounterManager manager;

    [Export(PropertyHint.Range, "0,60,0.1")]
    public float WaitSeconds { get; set; } = 5.0f;

    [Export]
    public Godot.Collections.Array<Node> PlayersToRegister { get; set; } = new();

    [Export]
    public Godot.Collections.Dictionary<ItemData, int>  StartingItems{get; set;} = new();


    public override async void _Ready()
    {
        if (manager == null)
        {
            GD.PrintErr("EncounterManager not assigned to BattleStarterTestScript in the editor.");
            return;
        }

        // Get the GameManager and register the player characters for this test run.
        var gameManager = GetNode<GameManager>(GameManager.Path);
        if (PlayersToRegister != null)
        {
            var playerPartyRegistered = gameManager.PlayerParty;
            foreach (var player in PlayersToRegister)
            {
                if (!gameManager.HasPlayerCharacter(player) && playerPartyRegistered.Find(p => p.Name == player.Name) == null)
                {
                    gameManager.AddPlayerCharacter(player);
                }
            }
        }

        var inventoryManager = GetNode<InventoryManager>(InventoryManager.Path);
        if (inventoryManager != null)
        {
            foreach (var kvp in StartingItems)
            {
                inventoryManager.AddItem(kvp.Key, kvp.Value);
            }
        }

        GD.Print($"Waiting for {WaitSeconds} seconds to check for an encounter...");

        // Create a one-shot timer and wait for it to complete.
        await ToSignal(GetTree().CreateTimer(WaitSeconds), SceneTreeTimer.SignalName.Timeout);

        GD.Print("Timer finished. Forcing encounter.");
        manager.ForceEncounter();
    }
}
