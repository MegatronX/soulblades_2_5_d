using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents a decision made by the AI for a turn.
/// </summary>
public partial class BattleDecision : RefCounted
{
    public ActionData Action { get; set; }
    public List<Node> Targets { get; set; } = new();

    public bool IsValid => Action != null && Targets.Count > 0;
}
