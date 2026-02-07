using Godot;

/// <summary>
/// An implementation of IMessageSink that writes messages to the Godot
/// debug console (`GD.Print`, `GD.PrintErr`, etc.).
/// </summary>
public class DebugLogSink : IMessageSink
{
    public void LogInfo(string message)
    {
        GD.Print(message);
    }

    public void LogWarning(string message)
    {
        GD.PrintRich($"[color=yellow]WARN: {message}[/color]");
    }

    public void LogError(string message)
    {
        GD.PrintErr(message);
    }
}