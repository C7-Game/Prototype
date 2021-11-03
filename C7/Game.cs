using Godot;
using System;
using System.Collections;
using ConvertCiv3Media;

public class Game : Node2D
{
	[Signal] public delegate void TurnStarted();
	[Signal] public delegate void TurnEnded();

	enum GameState {
		PreGame,
		PlayerTurn,
		ComputerTurn
	}

	public static readonly Vector2 tileSize = new Vector2(64, 32); // TODO: These should be integer values

	int mapWidth = 14, mapHeight = 18;
	int[,] Map;

	int cameraPixelX = 0, cameraPixelY = 0;
	int cameraTileX = 0, cameraTileY = 0;
	private TileMap MapView;

	Hashtable Terrmask = new Hashtable();
	GameState CurrentState = GameState.PreGame;
	Button EndTurnButton;
	Timer endTurnAlertTimer;
	private bool MoveCamera;
	private Vector2 OldPosition;
	private KinematicBody2D Player;
	
	LowerRightInfoBox LowerRightInfoBox = new LowerRightInfoBox();

	public int wrapTileX(int x)
	{
		int tr = x % mapWidth;
		return (tr >= 0) ? tr : tr + mapWidth;
	}

	public int wrapTileY(int y)
	{
		int tr = y % mapHeight;
		return (tr >= 0) ? tr : tr + mapHeight;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Player = GetNode<KinematicBody2D>("KinematicBody2D");
		this.TerrainAsTileMap();
		this.CreateUI();
		// If later recreating scene, the component may already exist, hence try/catch
		try{
			ComponentManager.Instance.AddComponent(new TurnCounterComponent());
		}
		catch {
			ComponentManager.Instance.GetComponent<TurnCounterComponent>().SetTurnCounter();
		}
		GD.Print("Now in game!");
	}

	public override void _Process(float delta)
	{
		switch (CurrentState)
		{
			case GameState.PreGame:
				StartGame();
				break;
			case GameState.PlayerTurn:
				break;
			case GameState.ComputerTurn:
				break;
		}
		//Listen to keys.  There is a C# Mono Godot bug where e.g. Godot.KeyList.F1 (etc.) doesn't work
		//without a manual cast to int.
		//https://github.com/godotengine/godot/issues/16388
		if (Input.IsKeyPressed(16777217))	//escape.  TODO: aka KEY_ESCAPE, which is global in GDScript but which I can't figure out how to import here.
		{
			GD.Print("User pressed escape");
			//TODO: Display the "Oh No! Do you really want to quit?" menu
		}
		else if (Input.IsKeyPressed((int)Godot.KeyList.F1)) {
			GD.Print("User requested domestic advisor");
			DomesticAdvisor advisor = new DomesticAdvisor();
			//TODO: Center on > 1024x768 res.
			AddChild(advisor);
		}
	}

	private void StartGame()
	{
		GD.Print("Game starting");
		TurnCounterComponent turnCntCpnt = ComponentManager.Instance.GetComponent<TurnCounterComponent>();
		Connect(nameof(TurnStarted), turnCntCpnt, nameof(turnCntCpnt.OnTurnStarted));
		Connect(nameof(TurnEnded), this, nameof(OnPlayerEndTurn));
		OnPlayerStartTurn();
	}

	public void refillMapView()
	{
		// TODO: Should use window size here but then need to resize the MapView when window size changes
		// The Offset of 4 is to provide a margin
		int mapViewWidth  = 4 + (int)(OS.GetScreenSize().x / MapView.CellSize.x);
		int mapViewHeight = 4 + (int)(OS.GetScreenSize().y / MapView.CellSize.y);

		// loop to place tiles, each of which contains 1/4 of 4 'real' map locations
		for (int y = 0; y < mapViewHeight; y++) {
			for (int x = 1 - (y%2); x < mapViewWidth; x+=2) {
				// TM.SetCellv(new Vector2(x + (y % 2), y), (new Random()).Next() % TS.GetTilesIds().Count);
				// try {
				MapView.SetCellv(new Vector2(x, y), Map[wrapTileX(cameraTileX+x), wrapTileY(cameraTileY+y)]);
				// } catch {}
			}
		}
	}

	public void TerrainAsTileMap() {
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		MapView = new TileMap();
		MapView.CellSize = tileSize;
		// TM.CenteredTextures = true;
		TileSet TS = new TileSet();
		MapView.TileSet = TS;

		Pcx PcxTxtr = new Pcx(Util.Civ3MediaPath("Art/Terrain/xpgc.pcx"));
		ImageTexture Txtr = PCXToGodot.getImageTextureFromPCX(PcxTxtr);

		int id = TS.GetLastUnusedTileId();
		for (int y = 0; y < PcxTxtr.Height; y += 64) {
			for (int x = 0; x < PcxTxtr.Width; x+= 128, id++) {
				TS.CreateTile(id);
				TS.TileSetTexture(id, Txtr);
				TS.TileSetRegion(id, new Rect2(x, y, 128, 64));
				// order right, bottom, left, top; 0 is plains, 1 grass, 2 coast
				Terrmask.Add(
					((y / 64) % 3).ToString("D3") +
					((y / 64) / 3 % 3).ToString("D3") +
					((x / 128) / 3 % 3).ToString("D3") +
					((x / 128) % 3).ToString("D3")
					, id);
			}
		}

		Map = new int[mapWidth,mapHeight];
		// Populate map values, 0 out terrain mask
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				// If x & y are both even or odd, terrain value; if mismatched, terrain mask init to 0
				Map[x,y] = x%2 - y%2 == 0 ? (new Random()).Next(0,3) : 0;
			}
		}
		// Loop to lookup tile ids based on terrain mask
		for (int y = 0; y < mapHeight; y++) {
			for (int x = (1 - (y % 2)); x < mapWidth; x+=2) {
				int Top = y == 0 ? (Map[(x+1) % mapWidth,y]) : (Map[x,y-1]);
				int Bottom = y == mapHeight - 1 ? (Map[(x+1) % mapWidth,y]) : (Map[x,y+1]);
				string foo = 
					(Map[(x+1) % mapWidth,y]).ToString("D3") +
					Bottom.ToString("D3") +
					(Map[Mathf.Abs((x-1) % mapWidth),y]).ToString("D3") +
					Top.ToString("D3")
				;
				try {
				// Map[x,y] = (int)Terrmask["001001001001"];
				Map[x,y] = (int)Terrmask[foo];
				} catch { GD.Print(x + "," + y + " " + foo); }
			}
		}
		refillMapView();
		AddChild(MapView);
	}

	private void CreateUI()
	{
		EndTurnButton = new Button();
		EndTurnButton.Text = "End Turn";
		EndTurnButton.SetPosition(new Vector2(250, 10));
		AddChild(EndTurnButton);
		EndTurnButton.Connect("pressed", this, "_onEndTurnButtonPressed");

		AddTopLeftButtons();
		AddLowerRightBox();
	}

	private void AddTopLeftButtons()
	{
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtons.pcx"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtonsAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 0, 1, 35, 29);
		TextureButton menuButton = new TextureButton();
		menuButton.TextureNormal = menuTexture;
		menuButton.SetPosition(new Vector2(21, 12));
		AddChild(menuButton);
		
		ImageTexture civilopediaTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 36, 1, 35, 29);
		TextureButton civilopediaButton = new TextureButton();
		civilopediaButton.TextureNormal = civilopediaTexture;
		civilopediaButton.SetPosition(new Vector2(57, 12));
		AddChild(civilopediaButton);
		
		ImageTexture advisorsTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 73, 1, 35, 29);
		TextureButton advisorsButton = new TextureButton();
		advisorsButton.TextureNormal = advisorsTexture;
		advisorsButton.SetPosition(new Vector2(94, 12));
		AddChild(advisorsButton);
	}

	private void AddLowerRightBox()
	{
		MarginContainer GameStatus = GetNode<MarginContainer>("CanvasLayer/GameStatus");
		//294 x 137 are the dimensions of the right info box.
		// LowerRightInfoBox.Position = (new Vector2(OS.WindowSize.x - (294 + 5), OS.WindowSize.y - (137 + 1)));
		GameStatus.MarginLeft = -(294 + 5);
		GameStatus.MarginTop = -(137 + 1);
		GameStatus.AddChild(LowerRightInfoBox);
	}

	private void _onEndTurnButtonPressed()
	{
		if (CurrentState == GameState.PlayerTurn)
		{
			OnPlayerEndTurn();
		}
		else
		{
			GD.Print("It's not your turn!");
		}
	}

	private void OnPlayerStartTurn()
	{
		GD.Print("Starting player turn");
		EmitSignal(nameof(TurnStarted));
		EndTurnButton.Disabled = false;
		CurrentState = GameState.PlayerTurn;

		//Set a timer so the end turn button starts blinking after awhile.
		//Obviously once we have more game mechanics, it won't happen automatically
		//after 5 seconds.
		endTurnAlertTimer = new Timer();
		endTurnAlertTimer.WaitTime = 5.0f;
		endTurnAlertTimer.OneShot = true;
		endTurnAlertTimer.Connect("timeout", LowerRightInfoBox, "toggleEndTurnButton");
		AddChild(endTurnAlertTimer);
		endTurnAlertTimer.Start();
	}

	private void OnPlayerEndTurn()
	{
		if (CurrentState == GameState.PlayerTurn)
		{
			GD.Print("Ending player turn");
			LowerRightInfoBox.StopToggling();
			EndTurnButton.Disabled = true;
			OnComputerStartTurn();
		}
	}

	private void OnComputerStartTurn()
	{
		GD.Print("Starting computer turn");
		CurrentState = GameState.ComputerTurn;
		ComputerSimulateTurn();
		GD.Print("Thinking...");
	}

	public async void ComputerSimulateTurn()
	{
		await ToSignal(GetTree().CreateTimer(2), "timeout");
		OnComputerEndTurn();
	}

	private void OnComputerEndTurn()
	{
		if (CurrentState == GameState.ComputerTurn)
		{
			GD.Print("Ending computer turn");
			OnPlayerStartTurn();
		}
	}

	public void _on_QuitButton_pressed()
	{
		// This apparently exits the whole program
		// GetTree().Quit();

		// ChangeScene deletes the current scene and frees its memory, so this is quitting to main menu
		GetTree().ChangeScene("res://MainMenu.tscn");    
	}

	public void _on_Zoom_value_changed(float value)
	{
		Vector2 NewScale = new Vector2(value, value);
		Scale = NewScale;
	}

	public void moveCamera(Vector2 offset)
	{
		cameraPixelX += (int)offset.x;
		cameraPixelY += (int)offset.y;

		int tileDoubleX = 2 * (int)tileSize.x;
		int tileDoubleY = 2 * (int)tileSize.y;

		// Renormalize X
		int tilesX = cameraPixelX / tileDoubleX;
		cameraPixelX -= tilesX * tileDoubleX;
		cameraTileX += 2 * tilesX;

		// Same for Y
		int tilesY = cameraPixelY / tileDoubleY;
		cameraPixelY -= tilesY * tileDoubleY;
		cameraTileY += 2 * tilesY;

		MapView.GlobalPosition = new Vector2(-cameraPixelX, -cameraPixelY);
		refillMapView();
	}

	public void _on_RightButton_pressed()
	{
		moveCamera(new Vector2(128, 0));
	}
	public void _on_LeftButton_pressed()
	{
		moveCamera(new Vector2(-128, 0));
	}
	public void _on_UpButton_pressed()
	{
		moveCamera(new Vector2(0, -64));
	}
	public void _on_DownButton_pressed()
	{
		moveCamera(new Vector2(0, 64));
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Scrolls map by repositioning "Player" when clicking & dragging mouse
		// Control node must not be in the way and/or have mouse pass enabled
		if(@event is InputEventMouseButton eventMouseButton)
		{
			if(eventMouseButton.ButtonIndex == (int)ButtonList.Left)
			{
				GetTree().SetInputAsHandled();
				if(eventMouseButton.IsPressed())
				{
					OldPosition = eventMouseButton.Position;
					MoveCamera = true;
				}
				else
				{
					MoveCamera = false;
				}
			}
			else if(eventMouseButton.ButtonIndex == (int)ButtonList.WheelUp)
			{
				GetTree().SetInputAsHandled();
				GetNode<HSlider>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/Zoom").Value += (float)0.1;
			}
			else if(eventMouseButton.ButtonIndex == (int)ButtonList.WheelDown)
			{
				GetTree().SetInputAsHandled();
				GetNode<HSlider>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/Zoom").Value -= (float)0.1;
			}
		}
		else if(@event is InputEventMouseMotion eventMouseMotion)
		{
			if(MoveCamera)
			{
				GetTree().SetInputAsHandled();
				moveCamera((OldPosition - eventMouseMotion.Position) / Scale);
				OldPosition = eventMouseMotion.Position;
			}
		}
	}

}
