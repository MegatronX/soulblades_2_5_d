using Godot;
using System;

public static class EventExtensions
{
    /// <summary>
    /// Subscribes to a C# event and automatically unsubscribes when the node exits the tree.
    /// This enforces the register/deregister pairing to prevent memory leaks.
    /// </summary>
    /// <param name="node">The node managing the subscription lifecycle.</param>
    /// <param name="subscribe">Action to perform subscription (e.g. () => bus.Event += Handler).</param>
    /// <param name="unsubscribe">Action to perform unsubscription (e.g. () => bus.Event -= Handler).</param>
    public static void Subscribe(this Node node, System.Action subscribe, System.Action unsubscribe)
    {
        subscribe();
        node.TreeExiting += () => unsubscribe();
    }
}

