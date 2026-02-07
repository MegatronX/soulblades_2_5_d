/// <summary>
/// Defines a contract for a message sink, which can direct log messages
/// to various outputs like the debug console, a UI, or a file.
/// </summary>
public interface IMessageSink
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
}