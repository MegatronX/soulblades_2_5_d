using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class BattleResultsOverlay : CanvasLayer
{
    public enum DefeatChoice
    {
        Retry,
        Quit
    }

    private Control _root;
    private ColorRect _backdrop;
    private PanelContainer _panel;
    private Label _titleLabel;
    private RichTextLabel _rewardsLabel;
    private RichTextLabel _eventsLabel;
    private VBoxContainer _buttonContainer;
    private Button _continueButton;
    private Button _retryButton;
    private Button _quitButton;

    private TaskCompletionSource<bool> _continueTcs;
    private TaskCompletionSource<DefeatChoice> _defeatTcs;
    private readonly List<System.Action> _unbindLevelEvents = new();

    public override void _Ready()
    {
        Layer = 200;
        BuildUi();
        HideOverlay();
    }

    public void ShowVictory(BattleRewards rewards, System.Action applyRewards, IEnumerable<Node> partyMembers)
    {
        ShowOverlay();
        BindLevelEvents(partyMembers);
        applyRewards?.Invoke();

        _titleLabel.Text = "Victory";
        _rewardsLabel.Text = BuildRewardsText(rewards);

        _continueButton.Visible = true;
        _retryButton.Visible = false;
        _quitButton.Visible = false;
        _continueButton.GrabFocus();
    }

    public void ShowDefeat(bool allowRetry)
    {
        _titleLabel.Text = "Defeat";
        _rewardsLabel.Text = "The party was defeated.";

        _continueButton.Visible = false;
        _retryButton.Visible = allowRetry;
        _quitButton.Visible = true;

        ShowOverlay();
        if (allowRetry)
        {
            _retryButton.GrabFocus();
        }
        else
        {
            _quitButton.GrabFocus();
        }
    }

    public Task WaitForContinueAsync()
    {
        _continueTcs = new TaskCompletionSource<bool>();
        return _continueTcs.Task;
    }

    public Task<DefeatChoice> WaitForDefeatChoiceAsync()
    {
        _defeatTcs = new TaskCompletionSource<DefeatChoice>();
        return _defeatTcs.Task;
    }

    public async Task FadeOutAsync(float duration = 0.2f)
    {
        var tween = CreateTween();
        tween.TweenProperty(_root, "modulate:a", 0.0f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
        UnbindLevelEvents();
        HideOverlay();
    }

    private void BuildUi()
    {
        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(_root);

        _backdrop = new ColorRect();
        _backdrop.Color = new Color(0, 0, 0, 0.6f);
        _backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _backdrop.MouseFilter = Control.MouseFilterEnum.Ignore;
        _root.AddChild(_backdrop);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        center.MouseFilter = Control.MouseFilterEnum.Ignore;
        _root.AddChild(center);

        _panel = new PanelContainer();
        _panel.CustomMinimumSize = new Vector2(520, 320);
        center.AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(vbox);

        _titleLabel = new Label();
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        vbox.AddChild(_titleLabel);

        _rewardsLabel = new RichTextLabel();
        _rewardsLabel.FitContent = true;
        _rewardsLabel.ScrollActive = false;
        vbox.AddChild(_rewardsLabel);

        _eventsLabel = new RichTextLabel();
        _eventsLabel.FitContent = true;
        _eventsLabel.ScrollActive = false;
        vbox.AddChild(_eventsLabel);

        _buttonContainer = new VBoxContainer();
        _buttonContainer.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(_buttonContainer);

        _continueButton = new Button();
        _continueButton.Text = "Continue";
        _continueButton.Pressed += OnContinuePressed;
        _buttonContainer.AddChild(_continueButton);

        _retryButton = new Button();
        _retryButton.Text = "Retry";
        _retryButton.Pressed += OnRetryPressed;
        _buttonContainer.AddChild(_retryButton);

        _quitButton = new Button();
        _quitButton.Text = "Quit";
        _quitButton.Pressed += OnQuitPressed;
        _buttonContainer.AddChild(_quitButton);
    }

    private void OnContinuePressed()
    {
        _continueTcs?.TrySetResult(true);
    }

    private void OnRetryPressed()
    {
        _defeatTcs?.TrySetResult(DefeatChoice.Retry);
    }

    private void OnQuitPressed()
    {
        _defeatTcs?.TrySetResult(DefeatChoice.Quit);
    }

    private void ShowOverlay()
    {
        _root.Show();
        _root.Modulate = new Color(1, 1, 1, 1);
    }

    private void HideOverlay()
    {
        _root.Hide();
    }

    private static string BuildRewardsText(BattleRewards rewards)
    {
        if (rewards == null)
        {
            return "No rewards.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"EXP: {rewards.TotalExperience}");
        sb.AppendLine($"AP: {rewards.TotalApExperience}");
        sb.AppendLine($"Money: {rewards.TotalMoney}");
        sb.AppendLine("");
        sb.AppendLine("Items:");

        if (rewards.Items.Count == 0)
        {
            sb.AppendLine("  None");
        }
        else
        {
            foreach (var kvp in rewards.Items.OrderBy(k => k.Key.ItemName))
            {
                sb.AppendLine($"  {kvp.Key.ItemName} x{kvp.Value}");
            }
        }

        return sb.ToString();
    }

    private void BindLevelEvents(IEnumerable<Node> partyMembers)
    {
        UnbindLevelEvents();
        if (_eventsLabel != null) _eventsLabel.Text = string.Empty;

        if (partyMembers == null) return;

        foreach (var member in partyMembers)
        {
            if (member == null) continue;
            var leveling = member.GetNodeOrNull<LevelingComponent>(LevelingComponent.NodeName);
            if (leveling == null) continue;

            string memberName = member.Name;

            LevelingComponent.LevelUpEventHandler levelUpHandler = (oldLevel, newLevel) =>
                AppendEventLine($"{memberName} reached Lv {newLevel}!");
            leveling.LevelUp += levelUpHandler;
            _unbindLevelEvents.Add(() => leveling.LevelUp -= levelUpHandler);

            LevelingComponent.StatIncreasedEventHandler statHandler = (statType, oldValue, newValue) =>
                AppendEventLine($"{memberName} {((StatType)statType)} {oldValue} -> {newValue}");
            leveling.StatIncreased += statHandler;
            _unbindLevelEvents.Add(() => leveling.StatIncreased -= statHandler);

            LevelingComponent.AbilityLearnedEventHandler abilityHandler = ability =>
                AppendEventLine($"{memberName} learned {ability.AbilityName}");
            leveling.AbilityLearned += abilityHandler;
            _unbindLevelEvents.Add(() => leveling.AbilityLearned -= abilityHandler);

            LevelingComponent.ActionLearnedEventHandler actionHandler = action =>
                AppendEventLine($"{memberName} learned {action.CommandName}");
            leveling.ActionLearned += actionHandler;
            _unbindLevelEvents.Add(() => leveling.ActionLearned -= actionHandler);
        }
    }

    private void UnbindLevelEvents()
    {
        foreach (var unbind in _unbindLevelEvents)
        {
            unbind();
        }
        _unbindLevelEvents.Clear();
    }

    private void AppendEventLine(string text)
    {
        if (_eventsLabel == null) return;
        if (string.IsNullOrEmpty(_eventsLabel.Text))
        {
            _eventsLabel.Text = text;
        }
        else
        {
            _eventsLabel.Text += "\n" + text;
        }
    }
}
