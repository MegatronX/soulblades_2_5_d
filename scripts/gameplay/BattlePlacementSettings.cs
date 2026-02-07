using Godot;
using System.Collections.Generic;

/// <summary>
/// Defines the layout and positioning logic for a battle.
/// Create different resources of this type for different arenas (e.g., Wide, Narrow, Ambush).
/// </summary>
[GlobalClass]
public partial class BattlePlacementSettings : Resource
{
    [Export] public float TeamXOffset { get; set; } = 3.0f; // Closer to center
    [Export] public float RowSpacingZ { get; set; } = 1.25f; // Tighter grouping
    [Export] public float RowStaggerX { get; set; } = 0.5f; // Slight diagonal stagger

    public void ApplyFormationPositions(List<Node> players, List<Node> enemies, List<Node> allies, BattleFormation formation)
    {
        // Combine players and allies for positioning usually
        var playerSideUnits = new List<Node>(players);
        playerSideUnits.AddRange(allies);

        Vector3 rightSidePos = new Vector3(TeamXOffset, 0, 0);
        Vector3 leftSidePos = new Vector3(-TeamXOffset, 0, 0);
        Vector3 centerPos = Vector3.Zero;

        // Direction vectors for the line formation
        // (X, Y, Z) - Stagger slightly back and outward
        Vector3 rightSideSpacing = new Vector3(RowStaggerX, 0, RowSpacingZ);
        Vector3 leftSideSpacing = new Vector3(-RowStaggerX, 0, RowSpacingZ);

        switch (formation)
        {
            case BattleFormation.Normal:
            case BattleFormation.PlayerAdvantage:
                // Players Right, Enemies Left
                PositionGroup(playerSideUnits, rightSidePos, rightSideSpacing);
                PositionGroup(enemies, leftSidePos, leftSideSpacing);
                break;

            case BattleFormation.EnemyAdvantage:
                // Players Left, Enemies Right
                PositionGroup(playerSideUnits, leftSidePos, leftSideSpacing);
                PositionGroup(enemies, rightSidePos, rightSideSpacing);
                break;

            case BattleFormation.Pincer:
                // Players Center (Straight line, no X stagger)
                PositionGroup(playerSideUnits, centerPos, new Vector3(0, 0, RowSpacingZ));

                // Enemies Split Left and Right
                int split = enemies.Count / 2;
                var group1 = enemies.GetRange(0, split);
                var group2 = enemies.GetRange(split, enemies.Count - split);

                PositionGroup(group1, leftSidePos, leftSideSpacing);
                PositionGroup(group2, rightSidePos, rightSideSpacing);
                break;
        }
    }

    private void PositionGroup(List<Node> units, Vector3 startPos, Vector3 spacingDir)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] is Node3D unit3D)
            {
                // Calculate offset. We center the group along the Z axis relative to the startPos
                // so the middle of the party is at startPos.Z
                float zOffset = (i * spacingDir.Z) - ((units.Count - 1) * spacingDir.Z / 2.0f);
                float xOffset = (i * spacingDir.X) - ((units.Count - 1) * spacingDir.X / 2.0f);

                unit3D.Position = startPos + new Vector3(xOffset, 0, zOffset);

                // Face the camera (assumed to be in the +Z direction)
                // We use Vector3.Back because in Godot, Back is (0, 0, 1).
                unit3D.LookAt(unit3D.Position + Vector3.Back, Vector3.Up);
            }
        }
    }
}