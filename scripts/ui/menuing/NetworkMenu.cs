using Godot;

/// <summary>
/// Handles the UI for creating or joining a network game.
/// </summary>
public partial class NetworkMenu : Control
{
	// You can set this path in the Godot editor inspector.
	[Export(PropertyHint.File, "*.tscn")]
	private string _gameScenePath;

	[Export]
	private LineEdit _nameEdit;
	[Export]
	private LineEdit _ipAddressEdit;
	[Export]
	private Button _hostButton;
	[Export]
	private Button _joinButton;
	[Export]
	private LineEdit _portEdit;
	[Export]
	private Label _statusLabel;

	private const int DefaultPort = 7777;

	public override void _Ready()
	{
		// Ensure nodes are assigned in the editor
		if (_hostButton == null || _joinButton == null || _nameEdit == null || _ipAddressEdit == null || _portEdit == null || _statusLabel == null)
		{
			GD.PrintErr("NetworkMenu is not configured correctly. Please assign all required nodes in the editor.");
			return;
		}

		_hostButton.Connect(Button.SignalName.Pressed, Callable.From(OnHostButtonPressed));
		_joinButton.Connect(Button.SignalName.Pressed, Callable.From(OnJoinButtonPressed));

		// Set default values for convenience, especially for local testing.
		_ipAddressEdit.Text = "127.0.0.1";
		_portEdit.Text = DefaultPort.ToString();

		// Using Godot's Connect is safer as it handles disconnection automatically.
		Multiplayer.Connect(MultiplayerApi.SignalName.ConnectedToServer, Callable.From(OnConnectedToServer));
		Multiplayer.Connect(MultiplayerApi.SignalName.ConnectionFailed, Callable.From(OnConnectionFailed));
		Multiplayer.Connect(MultiplayerApi.SignalName.ServerDisconnected, Callable.From(OnServerDisconnected));

		// Start with the UI in its default state.
		SetButtonsEnabled(true);
		_statusLabel.Visible = false;
	}

	public override void _ExitTree()
	{
		// No need to manually unsubscribe from signals connected via Connect().
		// Godot handles this automatically when the node is freed.
	}

	private void SetButtonsEnabled(bool enabled)
	{
		_hostButton.Disabled = !enabled;
		_joinButton.Disabled = !enabled;
	}

	private bool TryGetPort(out int port)
	{
		if (!int.TryParse(_portEdit.Text, out port))
		{
			GD.PrintErr("Invalid port: Not a number.");
			return false;
		}

		if (port <= 1024 || port > 65535)
		{
			GD.PrintErr("Invalid port: Must be between 1025 and 65535.");
			return false;
		}

		return true;
	}

	private void OnHostButtonPressed()
	{
		if (!TryGetPort(out int port)) return;
		var name = _nameEdit.Text;

		// Validate that the name is not empty.
		if (string.IsNullOrWhiteSpace(name))
		{
			_statusLabel.Text = "Name cannot be empty.";
			_statusLabel.Visible = true;
			return;
		}

		// Get the GameManager singleton
		var gameManager = GetNode<GameManager>(GameManager.Path);
		// Store the host's chosen name.
		gameManager.LocalPlayerName = name;

		GD.Print("Starting server...");
		SetButtonsEnabled(false);
		_statusLabel.Text = "Starting Server...";
		_statusLabel.Visible = true;
		gameManager.HostGame(port, 4); // Using a default of 4 max players
		
		// The server can load the game scene immediately.
		GetTree().ChangeSceneToFile(_gameScenePath);

		// After the scene change is initiated, tell the GameManager to finalize its setup.
		// We use CallDeferred to ensure this runs after the new scene is loaded.
		gameManager.CallDeferred(nameof(GameManager.FinalizeServerSetup)); // This now correctly calls the parameter-less version
	}

	private void OnJoinButtonPressed()
	{
		if (!TryGetPort(out int port)) return;
		var ip = _ipAddressEdit.Text;
		var name = _nameEdit.Text;

		// Validate that the name is not empty. 
		if (string.IsNullOrWhiteSpace(name))
		{
			_statusLabel.Text = "Name cannot be empty.";
			_statusLabel.Visible = true;
			return;
		}

		var gameManager = GetNode<GameManager>(GameManager.Path);
		// Store the chosen name in the GameManager so we can use it after connecting.
		gameManager.LocalPlayerName = name;

		GD.Print("Joining server...");
		SetButtonsEnabled(false);
		_statusLabel.Text = "Connecting...";
		_statusLabel.Visible = true;
		gameManager.JoinGame(ip, port);
		// We don't change scene here. We wait for the `ConnectedToServer` signal.
	}

	private void OnConnectedToServer()
	{
		GD.Print("Successfully connected to server!");
		// Now that we are connected, we can load the game scene.
		GetTree().ChangeSceneToFile(_gameScenePath);
	}

	private void OnConnectionFailed()
	{
		GD.PrintErr("Connection failed.");
		SetButtonsEnabled(true);
		_statusLabel.Text = "Connection Failed.";
		Multiplayer.MultiplayerPeer = null; // Clean up the failed peer
	}

	private void OnServerDisconnected()
	{
		GD.Print("Disconnected from server.");
		SetButtonsEnabled(true);
		_statusLabel.Text = "Disconnected.";
		Multiplayer.MultiplayerPeer = null; // Clean up
		// Optionally, return to the main menu scene here.
	}

	/// <summary>
	/// Sets the game window's resolution.
	/// </summary>
	/// <param name="width">The desired width in pixels.</param>
	/// <param name="height">The desired height in pixels.</param>
	private void SetResolution(int width, int height)
	{
		DisplayServer.WindowSetSize(new Vector2I(width, height));
		GD.Print($"Resolution changed to {width}x{height}");
	}

	private void ToggleFullscreen()
	{
		var currentMode = DisplayServer.WindowGetMode();
		DisplayServer.WindowSetMode(currentMode == DisplayServer.WindowMode.Fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
	}
}
