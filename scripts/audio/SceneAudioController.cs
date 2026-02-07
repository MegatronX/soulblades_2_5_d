using Godot;

/// <summary>
/// A generic audio controller for any scene (Battle, Map, Menu).
/// Handles background music playback and responds to global SFX events.
/// </summary>
public partial class SceneAudioController : Node
{
    public static SceneAudioController Instance { get; private set; }

    [Export] 
    public BattleMusicData DefaultMusic { get; set; }

    [Export]
    public bool AttemptToPlayBattleMusic { get; set; } = false;

    [Export]
    public float MusicCrossfadeDuration { get; set; } = 0.5f;

    private AudioStreamPlayer _musicPlayerA;
    private AudioStreamPlayer _musicPlayerB;
    private AudioStreamPlayer _currentMusicPlayer;
    private AudioStreamPlayer _lowHealthPlayer;
    private BattleMusicData _currentMusicData;
    private BattleMusicData _postBattleMusicData;
    private Tween _musicTween;

    private const float SilentVolumeDb = -80.0f;

    public override void _Ready()
    {
        Instance = this;

        _musicPlayerA = new AudioStreamPlayer();
        _musicPlayerA.Name = "BattleMusicPlayerA";
        // _musicPlayer.Bus = "Music"; 
        _musicPlayerA.Finished += () => OnMusicFinished(_musicPlayerA);
        AddChild(_musicPlayerA);

        _musicPlayerB = new AudioStreamPlayer();
        _musicPlayerB.Name = "BattleMusicPlayerB";
        _musicPlayerB.Finished += () => OnMusicFinished(_musicPlayerB);
        AddChild(_musicPlayerB);

        _currentMusicPlayer = _musicPlayerA;

        _lowHealthPlayer = new AudioStreamPlayer();
        _lowHealthPlayer.Name = "LowHealthMusicPlayer";
        _lowHealthPlayer.VolumeDb = SilentVolumeDb; // Start silent
        AddChild(_lowHealthPlayer);

        // Determine which music to play
        BattleMusicData musicToPlay = DefaultMusic;

        if (AttemptToPlayBattleMusic)
        {
            var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
            if (gameManager != null && gameManager.PendingBattleConfig?.BattleMusic != null)
            {
                musicToPlay = gameManager.PendingBattleConfig.BattleMusic;
            }
            if (gameManager != null && gameManager.PendingBattleConfig?.PostBattleMusic != null)
            {
                _postBattleMusicData = gameManager.PendingBattleConfig.PostBattleMusic;
            }
        }

        if (musicToPlay != null)
        {
            PlayMusic(musicToPlay);
        }

        if (AttemptToPlayBattleMusic && _postBattleMusicData != null)
        {
            var battleController = GetTree().Root.FindChild("BattleController", true, false) as BattleController;
            if (battleController != null)
            {
                this.Subscribe(
                    () => battleController.BattleEnded += OnBattleEnded,
                    () => battleController.BattleEnded -= OnBattleEnded
                );
            }
        }

        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        this.Subscribe(
            () => eventBus.PlaySFXRequested += OnPlaySFXRequested,
            () => eventBus.PlaySFXRequested -= OnPlaySFXRequested
        );
        this.Subscribe(
            () => eventBus.PlaySFX3DRequested += OnPlaySFX3DRequested,
            () => eventBus.PlaySFX3DRequested -= OnPlaySFX3DRequested
        );
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayMusic(BattleMusicData data)
    {
        _musicTween?.Kill();
        _currentMusicData = data;
        if (data == null || data.Stream == null) return;

        _musicPlayerA.Stop();
        _musicPlayerB.Stop();
        _currentMusicPlayer = _musicPlayerA;

        ConfigurePlayer(_currentMusicPlayer, data, data.VolumeDb);
        SetupLowHealthLayer(data, startSilent: true);

        _currentMusicPlayer.Play();
        if (_lowHealthPlayer.Stream != null) _lowHealthPlayer.Play();
    }

    public void SetLowHealthState(bool active, float fadeDuration = 1.0f)
    {
        if (_lowHealthPlayer.Stream == null || _currentMusicData == null) return;

        // Fade to the target volume if active, or silence if inactive
        float targetDb = active ? _currentMusicData.VolumeDb : SilentVolumeDb;
        var tween = CreateTween();
        tween.TweenProperty(_lowHealthPlayer, "volume_db", targetDb, fadeDuration);
    }

    private void OnBattleEnded(BattleController.BattleState result)
    {
        if (_postBattleMusicData == null) return;
        CrossfadeToMusic(_postBattleMusicData, MusicCrossfadeDuration);
    }

    public async void CrossfadeToMusic(BattleMusicData data, float duration)
    {
        if (data == null || data.Stream == null) return;

        if (_currentMusicPlayer == null || !_currentMusicPlayer.Playing || duration <= 0f)
        {
            PlayMusic(data);
            return;
        }

        _musicTween?.Kill();

        var oldPlayer = _currentMusicPlayer;
        var newPlayer = oldPlayer == _musicPlayerA ? _musicPlayerB : _musicPlayerA;

        ConfigurePlayer(newPlayer, data, SilentVolumeDb);
        newPlayer.Play();

        _musicTween = CreateTween();
        _musicTween.SetParallel(true);
        _musicTween.TweenProperty(oldPlayer, "volume_db", SilentVolumeDb, duration);
        _musicTween.TweenProperty(newPlayer, "volume_db", data.VolumeDb, duration);

        if (_lowHealthPlayer.Playing)
        {
            _musicTween.TweenProperty(_lowHealthPlayer, "volume_db", SilentVolumeDb, duration);
        }

        await ToSignal(_musicTween, Tween.SignalName.Finished);

        oldPlayer.Stop();
        oldPlayer.Stream = null;

        _currentMusicPlayer = newPlayer;
        _currentMusicData = data;

        SetupLowHealthLayer(data, startSilent: true);
        if (_lowHealthPlayer.Stream != null) _lowHealthPlayer.Play();
    }

    /// <summary>
    /// Plays a non-spatial (2D) sound effect.
    /// </summary>
    public void PlaySFX(AudioStream stream, float volumeDb = 0f, float pitchScale = 1.0f)
    {
        if (stream == null) return;
        var player = new AudioStreamPlayer();
        player.Stream = stream;
        player.VolumeDb = volumeDb;
        player.PitchScale = pitchScale;
        // player.Bus = "SFX";
        player.Finished += player.QueueFree;
        AddChild(player);
        player.Play();
    }

    /// <summary>
    /// Plays a spatial (3D) sound effect at a specific location.
    /// </summary>
    public void PlaySFX3D(AudioStream stream, Vector3 position, float volumeDb = 0f, float pitchScale = 1.0f)
    {
        if (stream == null) return;
        var player = new AudioStreamPlayer3D();
        player.Stream = stream;
        player.GlobalPosition = position;
        player.VolumeDb = volumeDb;
        player.PitchScale = pitchScale;
        // player.Bus = "SFX";
        player.Finished += player.QueueFree;
        AddChild(player);
        player.Play();
    }

    private void OnPlaySFXRequested(AudioStream stream, float volumeDb, float pitchScale)
    {
        PlaySFX(stream, volumeDb, pitchScale);
    }

    private void OnPlaySFX3DRequested(AudioStream stream, Vector3 position, float volumeDb, float pitchScale)
    {
        PlaySFX3D(stream, position, volumeDb, pitchScale);
    }

    private void OnMusicFinished(AudioStreamPlayer player)
    {
        if (_currentMusicData != null && _currentMusicData.Loop && player == _currentMusicPlayer)
        {
            player.Play();
            if (_lowHealthPlayer.Stream != null)
            {
                _lowHealthPlayer.Play();
            }
        }
    }

    private void ConfigurePlayer(AudioStreamPlayer player, BattleMusicData data, float volumeDb)
    {
        player.Stream = data.Stream;
        player.VolumeDb = volumeDb;
        player.PitchScale = data.PitchScale;
    }

    private void SetupLowHealthLayer(BattleMusicData data, bool startSilent)
    {
        if (data.LowHealthLayer != null)
        {
            _lowHealthPlayer.Stream = data.LowHealthLayer;
            _lowHealthPlayer.VolumeDb = startSilent ? SilentVolumeDb : data.VolumeDb;
            _lowHealthPlayer.PitchScale = data.PitchScale;
        }
        else
        {
            _lowHealthPlayer.Stop();
            _lowHealthPlayer.Stream = null;
        }
    }
}
