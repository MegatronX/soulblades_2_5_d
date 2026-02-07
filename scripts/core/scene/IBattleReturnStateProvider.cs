using Godot;

public interface IBattleReturnStateProvider
{
    Godot.Collections.Dictionary CaptureBattleReturnState();
    void RestoreBattleReturnState(Godot.Collections.Dictionary state);
}
