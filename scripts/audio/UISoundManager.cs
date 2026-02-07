using Godot;
using System.Collections.Generic;

public enum UISoundType
{
    Navigation,
    Confirm,
    Cancel,
    PageFlip,
    Invalid
}

/// <summary>
/// Manages playback of common UI sound effects.
/// Add this to your scene (or Autoload) and assign audio streams in the Inspector.
/// </summary>
public partial class UISoundManager : Node
{
    public static UISoundManager Instance { get; private set; }

    [Export] public string SoundBus { get; set; } = "Master";

    [Export] public UISoundTheme CurrentTheme { get; set; }

    [Export] public AudioStream NavigationSound { get; set; }
    [Export] public AudioStream ConfirmSound { get; set; }
    [Export] public AudioStream CancelSound { get; set; }
    [Export] public AudioStream PageFlipSound { get; set; }
    [Export] public AudioStream InvalidSound { get; set; }

    private List<AudioStreamPlayer> _playerPool = new();
    private const int InitialPoolSize = 8;

    public override void _Ready()
    {
        Instance = this;
        
        // Pre-instantiate a pool of players to avoid runtime instantiation lag.
        for (int i = 0; i < InitialPoolSize; i++)
        {
            AddPlayerToPool();
        }
    }

    private AudioStreamPlayer AddPlayerToPool()
    {
        var player = new AudioStreamPlayer();
        player.Bus = SoundBus;
        AddChild(player);
        _playerPool.Add(player);
        return player;
    }

    private AudioStreamPlayer GetAvailablePlayer()
    {
        foreach (var player in _playerPool)
        {
            if (!player.Playing)
            {
                return player;
            }
        }
        // Pool exhausted, expand it
        return AddPlayerToPool();
    }

    public void Play(UISoundType type)
    {
        AudioStream stream = null;

        // 1. Try to get sound from the current theme
        if (CurrentTheme != null)
        {
            stream = type switch
            {
                UISoundType.Navigation => CurrentTheme.NavigationSound,
                UISoundType.Confirm => CurrentTheme.ConfirmSound,
                UISoundType.Cancel => CurrentTheme.CancelSound,
                UISoundType.PageFlip => CurrentTheme.PageFlipSound,
                UISoundType.Invalid => CurrentTheme.InvalidSound,
                _ => null
            };
        }

        // 2. Fallback to default if theme is null or didn't provide a sound for this type
        if (stream == null)
        {
            stream = type switch
            {
                UISoundType.Navigation => NavigationSound,
                UISoundType.Confirm => ConfirmSound,
                UISoundType.Cancel => CancelSound,
                UISoundType.PageFlip => PageFlipSound,
                UISoundType.Invalid => InvalidSound,
                _ => null
            };
        }

        if (stream != null)
        {
            // Use a pooled player instead of creating a new one every time.
            var player = GetAvailablePlayer();
            player.Stream = stream;
            player.Bus = SoundBus;
            player.PitchScale = 1.0f; // Reset pitch in case it was modified
            player.Play();
        }
    }
}
