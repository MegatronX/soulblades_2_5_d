using Godot;
using System;

/// <summary>
/// Handles saving and loading of InputMap settings to a persistent file.
/// </summary>
public static class InputPersistenceManager
{
    private const string SavePath = "user://input_map.save";
    private const int MaxPlayerIndices = 4; // Support saving p0 through p3

    public static void Save()
    {
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"Failed to open {SavePath} for writing input config.");
            return;
        }

        var data = new Godot.Collections.Dictionary();

        // Iterate through all possible actions and indices we care about
        for (int i = 0; i < MaxPlayerIndices; i++)
        {
            foreach (GameInputAction action in Enum.GetValues(typeof(GameInputAction)))
            {
                string actionName = GameInputs.GetActionName(action, i);
                
                // Only save if the action exists in the map
                if (InputMap.HasAction(actionName))
                {
                    // Store the array of InputEvents (Key, JoypadButton, etc.)
                    data[actionName] = InputMap.ActionGetEvents(actionName);
                }
            }
        }

        // StoreVar handles serialization of Godot objects like InputEvent
        file.StoreVar(data);
    }

    public static void Load()
    {
        if (!FileAccess.FileExists(SavePath)) return;

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Failed to open {SavePath} for reading input config.");
            return;
        }

        var variant = file.GetVar();
        if (variant.VariantType != Variant.Type.Dictionary) return;

        var data = variant.AsGodotDictionary();

        foreach (var key in data.Keys)
        {
            string actionName = key.AsString();
            var events = data[key].AsGodotArray<InputEvent>();

            // Ensure the action exists before adding events
            if (!InputMap.HasAction(actionName))
            {
                InputMap.AddAction(actionName);
            }

            // Clear defaults/previous settings and apply loaded ones
            InputMap.ActionEraseEvents(actionName);

            foreach (var evt in events)
            {
                InputMap.ActionAddEvent(actionName, evt);
            }
        }
        
        GD.Print("Input mappings loaded successfully.");
    }
}
