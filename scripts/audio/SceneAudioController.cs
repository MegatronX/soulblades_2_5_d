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

    private AudioStreamPlayer _musicPlayer;
    private AudioStreamPlayer _lowHealthPlayer;
    private BattleMusicData _currentMusicData;

    public override void _Ready()
    {
        Instance = this;

        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Name = "BattleMusicPlayer";
        // _musicPlayer.Bus = "Music"; 
        _musicPlayer.Finished += OnMusicFinished;
        AddChild(_musicPlayer);

        _lowHealthPlayer = new AudioStreamPlayer();
        _lowHealthPlayer.Name = "LowHealthMusicPlayer";
        _lowHealthPlayer.VolumeDb = -80.0f; // Start silent
        AddChild(_lowHealthPlayer);

        // Determine which music to play
        BattleMusicData musicToPlay = DefaultMusic;

        if (AttemptToPlayBattleMusic)
        {
            var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
            if (gameManager != null && gameManager.PendingBattleMusic != null)
            {
                musicToPlay = gameManager.PendingBattleMusic;
            }
        }

        if (musicToPlay != null)
        {
            PlayMusic(musicToPlay);
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
        _currentMusicData = data;
        if (data.Stream == null) return;

        _musicPlayer.Stream = data.Stream;
        _musicPlayer.VolumeDb = data.VolumeDb;
        _musicPlayer.PitchScale = data.PitchScale;
        
        // Setup Low Health Layer
        if (data.LowHealthLayer != null)
        {
            _lowHealthPlayer.Stream = data.LowHealthLayer;
            _lowHealthPlayer.VolumeDb = -80.0f; // Ensure it starts silent
            _lowHealthPlayer.PitchScale = data.PitchScale;
        }
        else
        {
            _lowHealthPlayer.Stop();
            _lowHealthPlayer.Stream = null;
        }

        _musicPlayer.Play();
        if (_lowHealthPlayer.Stream != null) _lowHealthPlayer.Play();
    }

    public void SetLowHealthState(bool active, float fadeDuration = 1.0f)
    {
        if (_lowHealthPlayer.Stream == null || _currentMusicData == null) return;

        // Fade to the target volume if active, or silence if inactive
        float targetDb = active ? _currentMusicData.VolumeDb : -80.0f;
        var tween = CreateTween();
        tween.TweenProperty(_lowHealthPlayer, "volume_db", targetDb, fadeDuration);
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

    private void OnMusicFinished()
    {
        if (_currentMusicData != null && _currentMusicData.Loop)
        {
            _musicPlayer.Play();
            if (_lowHealthPlayer.Stream != null)
            {
                _lowHealthPlayer.Play();
            }
        }
    }
}