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

    private enum VictoryStage
    {
        Summary,
        Party
    }

    [Export] private NodePath _rootPath;
    [Export] private NodePath _backdropPath;
    [Export] private NodePath _titleLabelPath;
    [Export] private NodePath _summaryContainerPath;
    [Export] private NodePath _rewardsLabelPath;
    [Export] private NodePath _rewardsSummaryLabelPath;
    [Export] private NodePath _rewardsItemsLabelPath;
    [Export] private NodePath _partyContainerPath;
    [Export] private NodePath _partyListContainerPath;
    [Export] private NodePath _eventsLabelPath;
    [Export] private NodePath _continueButtonPath;
    [Export] private NodePath _retryButtonPath;
    [Export] private NodePath _quitButtonPath;
    [Export] private NodePath _panelPath;
    [Export] private NodePath _buttonContainerPath;
    [Export] private NodePath _rewardsSummaryPanelPath;
    [Export] private NodePath _rewardsItemsPanelPath;
    [Export] private PackedScene _partyRowScene;

    [ExportGroup("Animation")]
    [Export] private float _introFadeSeconds = 0.3f;
    [Export] private float _introSlideSeconds = 0.25f;
    [Export] private float _introSlidePixels = 18.0f;
    [Export] private float _introStaggerSeconds = 0.08f;
    [Export] private float _rewardCountSeconds = 0.6f;
    [Export] private float _panelPulseSeconds = 0.25f;
    [Export] private float _rowRevealSeconds = 0.42f;
    [Export] private float _rowRevealStaggerSeconds = 0.15f;
    [Export] private float _rowSlidePixels = 40f;
    [Export] private float _rowSlideOvershootPixels = 6f;
    [Export] private float _rowSlideSettleSeconds = 0.18f;
    [Export] private bool _rowSlideFromOffscreen = true;
    [Export] private float _rowSlideOffscreenPadding = 80f;
    [Export] private bool _rowSlideElastic = false;
    [Export] private float _expPerSecond = 140f;
    [Export] private float _expSpeedScaleUnit = 100f;
    [Export] private float _expSpeedScaleMin = 0.6f;
    [Export] private float _expSpeedScaleMax = 3.0f;
    [Export] private float _expMinSegmentSeconds = 0.2f;
    [Export] private float _expMaxSegmentSeconds = 1.0f;
    [Export] private float _levelUpPauseSeconds = 0.08f;
    [Export] private float _expBannerDelaySeconds = 0.08f;

    private Control _root;
    private ColorRect _backdrop;
    private Label _titleLabel;
    private VBoxContainer _summaryContainer;
    private RichTextLabel _rewardsLabel;
    private RichTextLabel _rewardsSummaryLabel;
    private RichTextLabel _rewardsItemsLabel;
    private VBoxContainer _partyContainer;
    private VBoxContainer _partyListContainer;
    private RichTextLabel _eventsLabel;
    private Button _continueButton;
    private Button _retryButton;
    private Button _quitButton;
    private Control _panel;
    private Control _buttonContainer;
    private Control _rewardsSummaryPanel;
    private Control _rewardsItemsPanel;
    private Vector2 _panelBasePosition;

    private TaskCompletionSource<bool> _continueTcs;
    private TaskCompletionSource<DefeatChoice> _defeatTcs;
    private readonly List<System.Action> _unbindLevelEvents = new();

    private VictoryStage _victoryStage;
    private readonly List<PartySnapshot> _partySnapshots = new();
    private readonly List<PartyRow> _partyRows = new();
    private readonly Dictionary<Node, PartyRow> _partyRowsByMember = new();
    private readonly Dictionary<Node, List<string>> _pendingRowBanners = new();
    private readonly Dictionary<Node, List<string>> _pendingLevelRewardBanners = new();
    private bool _canShowBanners;
    private bool _isAnimatingExp;
    private bool _skipExpAnimation;
    private bool _rewardsApplied;
    private System.Action _pendingApplyRewards;
    private IEnumerable<Node> _pendingPartyMembers;

    public override void _Ready()
    {
        Layer = 200;
        CacheNodes();
        HookButtons();
        HideOverlay();
    }

    public void ShowVictory(BattleRewards rewards, System.Action applyRewards, IEnumerable<Node> partyMembers)
    {
        ShowOverlay();
        _pendingApplyRewards = applyRewards;
        _pendingPartyMembers = partyMembers;
        _rewardsApplied = false;
        _pendingRowBanners.Clear();
        _pendingLevelRewardBanners.Clear();
        _canShowBanners = false;
        CapturePartyStart(partyMembers);
        if (_eventsLabel != null) _eventsLabel.Text = string.Empty;

        _titleLabel.Text = "Victory";
        if (_rewardsSummaryLabel != null && _rewardsItemsLabel != null)
        {
            _rewardsSummaryLabel.Text = BuildRewardsSummaryText(0, 0, 0);
            _rewardsItemsLabel.Text = BuildRewardsItemsText(rewards);
        }
        else if (_rewardsLabel != null)
        {
            _rewardsLabel.Text = BuildRewardsText(rewards);
        }

        _continueButton.Text = "Continue";
        _continueButton.Visible = true;
        _retryButton.Visible = false;
        _quitButton.Visible = false;

        _summaryContainer.Show();
        _partyContainer.Hide();
        if (_eventsLabel != null) _eventsLabel.Hide();

        _victoryStage = VictoryStage.Summary;
        _continueButton.GrabFocus();

        _ = PlayVictoryIntroAsync(rewards);
    }

    public void ShowDefeat(bool allowRetry)
    {
        _titleLabel.Text = "Defeat";
        if (_rewardsSummaryLabel != null && _rewardsItemsLabel != null)
        {
            _rewardsSummaryLabel.Text = "[center]The party was defeated.[/center]";
            _rewardsItemsLabel.Text = string.Empty;
        }
        else if (_rewardsLabel != null)
        {
            _rewardsLabel.Text = "The party was defeated.";
        }

        _continueButton.Visible = false;
        _retryButton.Visible = allowRetry;
        _quitButton.Visible = true;

        _summaryContainer.Show();
        _partyContainer.Hide();

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
        if (_root == null)
        {
            HideOverlay();
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(_root, "modulate:a", 0.0f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
        UnbindLevelEvents();
        HideOverlay();
    }

    private void CacheNodes()
    {
        _root = GetNodeOrNull<Control>(_rootPath);
        _backdrop = GetNodeOrNull<ColorRect>(_backdropPath);
        _titleLabel = GetNodeOrNull<Label>(_titleLabelPath);
        _summaryContainer = GetNodeOrNull<VBoxContainer>(_summaryContainerPath);
        _rewardsLabel = GetNodeOrNull<RichTextLabel>(_rewardsLabelPath);
        _rewardsSummaryLabel = GetNodeOrNull<RichTextLabel>(_rewardsSummaryLabelPath);
        _rewardsItemsLabel = GetNodeOrNull<RichTextLabel>(_rewardsItemsLabelPath);
        _partyContainer = GetNodeOrNull<VBoxContainer>(_partyContainerPath);
        _partyListContainer = GetNodeOrNull<VBoxContainer>(_partyListContainerPath);
        _eventsLabel = GetNodeOrNull<RichTextLabel>(_eventsLabelPath);
        _continueButton = GetNodeOrNull<Button>(_continueButtonPath);
        _retryButton = GetNodeOrNull<Button>(_retryButtonPath);
        _quitButton = GetNodeOrNull<Button>(_quitButtonPath);
        _panel = GetNodeOrNull<Control>(_panelPath);
        _buttonContainer = GetNodeOrNull<Control>(_buttonContainerPath);
        _rewardsSummaryPanel = GetNodeOrNull<Control>(_rewardsSummaryPanelPath);
        _rewardsItemsPanel = GetNodeOrNull<Control>(_rewardsItemsPanelPath);

        if (_panel != null) _panelBasePosition = _panel.Position;

        if (_root == null) GD.PrintErr("BattleResultsOverlay: Root path not set or invalid.");
        if (_titleLabel == null) GD.PrintErr("BattleResultsOverlay: TitleLabel path not set or invalid.");
        if (_summaryContainer == null) GD.PrintErr("BattleResultsOverlay: SummaryContainer path not set or invalid.");
        if (_rewardsLabel == null && (_rewardsSummaryLabel == null || _rewardsItemsLabel == null))
            GD.PrintErr("BattleResultsOverlay: Rewards labels not set or invalid.");
        if (_partyContainer == null) GD.PrintErr("BattleResultsOverlay: PartyContainer path not set or invalid.");
        if (_partyListContainer == null) GD.PrintErr("BattleResultsOverlay: PartyListContainer path not set or invalid.");
        if (_eventsLabel == null) GD.PrintErr("BattleResultsOverlay: EventsLabel path not set or invalid.");
        if (_continueButton == null) GD.PrintErr("BattleResultsOverlay: ContinueButton path not set or invalid.");
        if (_retryButton == null) GD.PrintErr("BattleResultsOverlay: RetryButton path not set or invalid.");
        if (_quitButton == null) GD.PrintErr("BattleResultsOverlay: QuitButton path not set or invalid.");
    }

    private void HookButtons()
    {
        _continueButton.Pressed += OnContinuePressed;
        _retryButton.Pressed += OnRetryPressed;
        _quitButton.Pressed += OnQuitPressed;
    }

    private void OnContinuePressed()
    {
        if (_victoryStage == VictoryStage.Summary)
        {
            ApplyPendingRewards();
            _victoryStage = VictoryStage.Party;
            _summaryContainer.Hide();
            _partyContainer.Show();
            if (_eventsLabel != null) _eventsLabel.Show();
            _continueButton.Text = "Continue";
            _continueButton.GrabFocus();
            _ = AnimatePartyExpAsync();
            return;
        }

        if (_isAnimatingExp)
        {
            _skipExpAnimation = true;
            return;
        }

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
        if (_root == null)
        {
            Show();
            return;
        }
        _root.Show();
        _root.Modulate = new Color(1, 1, 1, 1);
    }

    private void HideOverlay()
    {
        if (_root == null)
        {
            Hide();
            return;
        }
        _root.Hide();
    }

    private void ApplyPendingRewards()
    {
        if (_rewardsApplied) return;
        _rewardsApplied = true;
        _canShowBanners = false;

        if (_pendingPartyMembers != null)
        {
            BindLevelEvents(_pendingPartyMembers);
        }

        _pendingApplyRewards?.Invoke();

        if (_pendingPartyMembers != null)
        {
            CapturePartyEnd(_pendingPartyMembers);
        }
    }

    private static string GetDisplayName(Node member)
    {
        if (member is BaseCharacter baseCharacter)
        {
            var displayName = baseCharacter.PresentationData?.DisplayName;
            if (!string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }
        }
        return member.Name;
    }

    private static Texture2D GetPortraitTexture(Node member)
    {
        if (member is BaseCharacter baseCharacter)
        {
            return baseCharacter.PresentationData?.PortraitImage;
        }
        return null;
    }
}
