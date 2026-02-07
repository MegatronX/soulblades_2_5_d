using System;
using Godot;

/// <summary>
/// A UI panel for equipping and unequipping abilities from a character's known list.
/// </summary>
public partial class AbilityPanel : PanelContainer
{
    [Export] private ItemList _knownList;
    [Export] private ItemList _equippedList;
    [Export] private Button _equipButton;
    [Export] private Button _unequipButton;
    [Export] private Label _apLabel;

    // This should be set to the AbilityManager of the character being viewed.
    private AbilityManager _abilityManager;
    private StatsComponent _statsComponent;

    public void Initialize(AbilityManager abilityManager, StatsComponent statsComponent)
    {
        _abilityManager = abilityManager;
        _statsComponent = statsComponent;

        // Connect to signals to automatically update the UI.
        _abilityManager.KnownAbilitiesChanged += RefreshKnownList;
        _abilityManager.EquippedAbilitiesChanged += RefreshEquippedList;
        _abilityManager.EquippedAbilitiesChanged += RefreshApLabel;
        _statsComponent.StatValueChanged += OnStatValueChanged;

        // Initial population of the UI.
        RefreshAll();
    }

    public override void _ExitTree()
    {
        // It's crucial to unsubscribe from events to prevent memory leaks.
        if (_abilityManager != null)
        {
            _abilityManager.KnownAbilitiesChanged -= RefreshKnownList;
            _abilityManager.EquippedAbilitiesChanged -= RefreshEquippedList;
            _abilityManager.EquippedAbilitiesChanged -= RefreshApLabel;
        }
        if (_statsComponent != null)
        {
            _statsComponent.StatValueChanged -= OnStatValueChanged;
        }

        // We also need to disconnect from the button's Pressed signal.
        _equipButton.Pressed -= OnEquipButtonPressed;
        _unequipButton.Pressed -= OnUnequipButtonPressed;
    }

    public override void _Ready()
    {
        _equipButton.Pressed += OnEquipButtonPressed;
        _unequipButton.Pressed += OnUnequipButtonPressed;
    }

    private void RefreshAll()
    {
        RefreshKnownList();
        RefreshEquippedList();
        RefreshApLabel();
    }

    private void RefreshKnownList()
    {
        _knownList.Clear();
        foreach (var ability in _abilityManager.GetKnownAbilities())
        {
            _knownList.AddItem($"{ability.AbilityName} ({ability.ApCost} AP)");
            _knownList.SetItemMetadata(_knownList.ItemCount - 1, ability);
        }
    }

    private void RefreshEquippedList()
    {
        _equippedList.Clear();
        foreach (var ability in _abilityManager.GetEquippedAbilities())
        {
            _equippedList.AddItem($"{ability.AbilityName} ({ability.ApCost} AP)");
            _equippedList.SetItemMetadata(_equippedList.ItemCount - 1, ability);
        }
    }

    private void RefreshApLabel()
    {
        int currentAp = _abilityManager.GetCurrentApCost();
        int maxAp = (int)_statsComponent.GetStatValue(StatType.AP);
        _apLabel.Text = $"AP: {currentAp} / {maxAp}";
    }

    private void OnEquipButtonPressed()
    {
        var selectedIndices = _knownList.GetSelectedItems();
        if (selectedIndices.Length == 0) return;

        var ability = _knownList.GetItemMetadata(selectedIndices[0]).As<Ability>();
        if (ability != null)
        {
            _abilityManager.EquipAbility(ability);
        }
    }

    private void OnUnequipButtonPressed()
    {
        var selectedIndices = _equippedList.GetSelectedItems();
        if (selectedIndices.Length == 0) return;

        var ability = _equippedList.GetItemMetadata(selectedIndices[0]).As<Ability>();
        if (ability != null)
        {
            _abilityManager.UnequipAbility(ability);
        }
    }

    private void OnStatValueChanged(long statTypeLong, int newValue)
    {
        var statType = (StatType)statTypeLong;

        // If the character's max AP changes, refresh the label.
        if (statType == StatType.AP)
        {
            RefreshApLabel();
        }
    }
}