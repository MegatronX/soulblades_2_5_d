using System.Collections.Generic;
using Godot;

/// <summary>
/// A test harness scene for developing and debugging the EquipmentMenu.
/// It creates a mock character and inventory to provide a functional test environment.
/// </summary>
public partial class EquipmentMenuTest : Node
{
    [Export]
    private EquipmentMenu _equipmentMenu;

    [Export]
    private BaseStats _mockCharacterStats;

    private Node _mockCharacter;
    private InventoryManager _inventoryManager;

    public override void _Ready()
    {
        // Get the singleton instances we need.
        _inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");

        // 1. Create the mock character and its components.
        _mockCharacter = CreateMockCharacter("Hero");
        AddChild(_mockCharacter); // Add to the scene tree so it's a valid node.

        // 2. Create some mock items to test with.
        var sword = CreateMockEquippableItem("Iron Sword", EquipmentSlotType.Weapon, new List<StatModifier>
        {
            new StatModifier(StatType.Strength, 10, ModifierType.Additive, null),
            new StatModifier(StatType.HP, 50, ModifierType.Additive, null),
            new StatModifier(StatType.Defense, 3, ModifierType.Additive, null)
        });
        var betterSword = CreateMockEquippableItem("Steel Sword", EquipmentSlotType.Weapon, new List<StatModifier>
        {
            new StatModifier(StatType.Strength, 20, ModifierType.Additive, null),
            new StatModifier(StatType.HP, 80, ModifierType.Additive, null),
            new StatModifier(StatType.Defense, 5, ModifierType.Additive, null)
        });
        var helmet = CreateMockEquippableItem("Iron Helm", EquipmentSlotType.Head, new List<StatModifier>
        {
            new StatModifier(StatType.HP, 50, ModifierType.Additive, null),
            new StatModifier(StatType.Defense, 13, ModifierType.Additive, null)
        });
        var ring = CreateMockEquippableItem("Ring of Power", EquipmentSlotType.Accessory, new List<StatModifier>
        {
            new StatModifier(StatType.Strength, 20, ModifierType.Additive, null),
            new StatModifier(StatType.Strength, 2, ModifierType.Multiplicative, null),
            new StatModifier(StatType.Magic, 20, ModifierType.Additive, null),
        });
        var potion = CreateMockConsumableItem("Health Potion"); // To test filtering.

        // 3. Add the mock items to the player's inventory.
        _inventoryManager.AddItem(sword);
        _inventoryManager.AddItem(betterSword);
        _inventoryManager.AddItem(helmet);
        _inventoryManager.AddItem(ring);
        _inventoryManager.AddItem(potion);

        // 4. Open the equipment menu for our mock character.
        _equipmentMenu.OpenForCharacter(_mockCharacter);
    }

    private Node CreateMockCharacter(string name)
    {
        var character = new Node2D { Name = name };
        
        var equipmentManager = new EquipmentManager { Name = "EquipmentManager" };
        character.AddChild(equipmentManager);

        // Add equipment slots as children of the manager.
        // The names here ("Weapon", "Head", etc.) must match the names of the
        // EquipmentSlotUI buttons in your EquipmentMenu scene.
        var weaponSlot = new EquipmentSlot { Name = "Weapon", SlotType = EquipmentSlotType.Weapon };
        var headSlot = new EquipmentSlot { Name = "Head", SlotType = EquipmentSlotType.Head };
        var accessorySlot1 = new EquipmentSlot { Name = "Accessory1", SlotType = EquipmentSlotType.Accessory };
        var accessorySlot2 = new EquipmentSlot { Name = "Accessory2", SlotType = EquipmentSlotType.Accessory };

        equipmentManager.AddChild(weaponSlot);
        equipmentManager.AddChild(headSlot);
        equipmentManager.AddChild(accessorySlot1);
        equipmentManager.AddChild(accessorySlot2);

        var statsComponent = new StatsComponent { Name = "StatsComponent" };
        statsComponent.SetBaseStatsResource(_mockCharacterStats);
        character.AddChild(statsComponent);


        return character;
    }

    private ItemData CreateMockEquippableItem(string name, EquipmentSlotType slotType, List<StatModifier> modifiers = null)
    {
        var equippableComponent = new EquippableComponentData();
        equippableComponent.SlotType = slotType;
        var item = new ItemData();
        item.ItemName = name;

        modifiers?.ForEach(m =>
        {
            if (m.Source == null)
            {
                m.Source = item;
            }
            equippableComponent.StatBoosts.Add(m);
        }
        );

        item.Description = $"A basic {name}.";
        item.Components.Add(equippableComponent);
        return item;
    }

    private ItemData CreateMockConsumableItem(string name)
    {
        var item = new ItemData();
        item.Components.Add(new ConsumableComponentData());
        return item;
    }
}