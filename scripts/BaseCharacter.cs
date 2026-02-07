using Godot;

/// <summary>
/// A base class for all combatants in the game, both players and enemies.
/// It holds common components like StatsComponent and presentation data.
/// </summary>
public partial class BaseCharacter : CharacterBody3D
{
    [Export]
    public CharacterPresentationData PresentationData { get; private set; }

    [Export]
    public AnimationPlayer AnimationPlayer { get; private set; }

    [Export]
    public AnimationTree AnimationTree { get; private set; }

    public override void _Ready()
    {
        // Fallback to finding nodes by name if not assigned in the editor
        if (AnimationPlayer == null) AnimationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (AnimationTree == null) AnimationTree = GetNodeOrNull<AnimationTree>("AnimationTree");

        // Randomize speed for non-player characters (Enemies/Allies)
        // This gives a more organic feel to groups of enemies.
        if (!IsInGroup(GameGroups.PlayerCharacters))
        {
            float randomSpeed = (float)GD.RandRange(0.9, 1.1);
            if (AnimationPlayer != null) AnimationPlayer.SpeedScale = randomSpeed;
        }

        // Randomize start frame
        if (AnimationTree != null && AnimationTree.Active)
        {
            // Advance the tree simulation by a random amount
            AnimationTree.Advance((float)GD.RandRange(0.0, 2.0));
        }
        else if (AnimationPlayer != null)
        {
            if (AnimationPlayer.IsPlaying())
            {
                AnimationPlayer.Seek(GD.Randf() * AnimationPlayer.CurrentAnimationLength, true);
            }
            else if (!string.IsNullOrEmpty(AnimationPlayer.Autoplay))
            {
                string animName = AnimationPlayer.Autoplay;
                if (AnimationPlayer.HasAnimation(animName))
                {
                    AnimationPlayer.Play(animName);
                    AnimationPlayer.Seek(GD.Randf() * AnimationPlayer.GetAnimation(animName).Length, true);
                }
            }
        }
    }
}