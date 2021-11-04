using Godot;
using System;
using System.Collections;
using ConvertCiv3Media;

public class Game : Node2D
{
	[Signal] public delegate void TurnStarted();
	[Signal] public delegate void TurnEnded();
	[Signal] public delegate void HideAdvisor();

	enum GameState {
		PreGame,
		PlayerTurn,
		ComputerTurn
	}

	public static readonly Vector2 tileSize = new Vector2(64, 32); // TODO: These should be integer values

	bool mapWrapHorizontally = false, mapWrapVertically = false;
	int mapWidth = 80, mapHeight = 80;
	int[,] Map;

	// cameraLocation stores the upper left pixel coordinates on the map of the area currently being viewed.
	Vector2 cameraLocation = new Vector2(0, 0);
	private TileMap MapView;

	Hashtable Terrmask = new Hashtable();
	GameState CurrentState = GameState.PreGame;
	Button EndTurnButton;
	Control Toolbar;
	
	CenterContainer AdvisorContainer;
	Timer endTurnAlertTimer;
	private bool IsMovingCamera;
	private Vector2 OldPosition;
	private KinematicBody2D Player;
	
	LowerRightInfoBox LowerRightInfoBox = new LowerRightInfoBox();

	public bool IsInRange(int x, int y)
	{
		bool xInRange = mapWrapHorizontally || ((x >= 0) && (x < mapWidth));
		bool yInRange = mapWrapVertically   || ((y >= 0) && (y < mapHeight));
		return xInRange && yInRange;
	}

	public int WrapTileX(int x)
	{
		int tr = x % mapWidth;
		return (tr >= 0) ? tr : tr + mapWidth;
	}

	public int WrapTileY(int y)
	{
		int tr = y % mapHeight;
		return (tr >= 0) ? tr : tr + mapHeight;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Toolbar = GetNode<Control>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer");
		Player = GetNode<KinematicBody2D>("KinematicBody2D");
		GetTree().Root.Connect("size_changed", this, "_OnViewportSizeChanged");
		this.TerrainAsTileMap();
		MapView.Scale = new Vector2((float)0.3, (float)0.3);
		RefillMapView(); // Reset view after setting scale
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
			ShowDomesticAdvisor();
		}
	}

	private void StartGame()
	{
		GD.Print("Game starting");
		TurnCounterComponent turnCntCpnt = ComponentManager.Instance.GetComponent<TurnCounterComponent>();
		Connect(nameof(TurnStarted), turnCntCpnt, nameof(turnCntCpnt.OnTurnStarted));
		Connect(nameof(TurnEnded), this, nameof(OnPlayerEndTurn));
		Connect(nameof(HideAdvisor), this, nameof(OnHideAdvisor));
		OnPlayerStartTurn();
	}

	public void RefillMapView()
	{
		MapView.Clear();

		// MapView is not the entire game map, rather it is a window into the game map that stays near the origin and covers the entire
		// screen. For small movements, the MapView itself is moved (amount is in cameraResidueX/Y) but once the movement equals an entire
		// grid cell (2 times the tile width or height) the map is snapped back toward the origin by that amount and to compensate it changes
		// what tiles are drawn (cameraTileX/Y). The advantage to doing things this way is that it makes it easy to duplicate tiles around
		// wrapped edges.

		Vector2 tileFullSize = 2 * MapView.Scale * tileSize;

		int cameraPixelX = (int)cameraLocation.x;
		int fullTilesX = cameraPixelX / (int)tileFullSize.x;
		int cameraTileX = 2 * fullTilesX;
		int cameraResidueX = cameraPixelX - fullTilesX * (int)tileFullSize.x;

		int cameraPixelY = (int)cameraLocation.y;
		int fullTilesY = cameraPixelY / (int)tileFullSize.y;
		int cameraTileY = 2 * fullTilesY;
		int cameraResidueY = cameraPixelY - fullTilesY * (int)tileFullSize.y;

		MapView.GlobalPosition = new Vector2(-cameraResidueX, -cameraResidueY);

		// The Offset of 2 is to provide a margin
		int mapViewWidth  = 2 + (int)(OS.WindowSize.x / (MapView.Scale.x * MapView.CellSize.x));
		int mapViewHeight = 2 + (int)(OS.WindowSize.y / (MapView.Scale.y * MapView.CellSize.y));

		// loop to place tiles, each of which contains 1/4 of 4 'real' map locations
		// loops start at -3 and -6 to provide a margin on the left and top, respectively
		for (int dy = -6; dy < mapViewHeight; dy++) {
			for (int dx = -3 - (dy%2); dx < mapViewWidth; dx+=2) {
				int x = cameraTileX + dx, y = cameraTileY + dy;
				if (IsInRange(x, y)) {
					MapView.SetCell(dx, dy, Map[WrapTileX(x), WrapTileY(y)]);
				}
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
		OpenSimplexNoise noise = new OpenSimplexNoise();
		noise.Seed = (new Random()).Next(int.MinValue, int.MaxValue);
		// Populate map values
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				// Multiplying x & y for noise coordinate sampling
				float foo = noise.GetNoise2d(x*2,y*2);
				Map[x,y] = foo < 0.1 ? 2 : foo < 0.4? 1 : 0;
			}
		}
		// Loop to lookup tile ids based on terrain mask
		//  NOTE: This layout is full width, but the tiles are every-other coordinate
		//    What I've done is generated "terrain ID" all over and am deriving an
		//    image ID on the tile placement spots based on surrounding terrain values
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
		RefillMapView();
		AddChild(MapView);
	}

	private void CreateUI()
	{
		EndTurnButton = new Button();
		EndTurnButton.Text = "End Turn";
		EndTurnButton.SetPosition(new Vector2(250, 10));
		Toolbar.AddChild(EndTurnButton);
		Toolbar.MoveChild(EndTurnButton, 0);
		EndTurnButton.Connect("pressed", this, "_onEndTurnButtonPressed");

		AddTopLeftButtons();
		AddLowerRightBox();
	}

	private void AddTopLeftButtons()
	{
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtons.pcx"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtonsAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 0, 1, 35, 29);
		
		TextureButton menuButton = GetNode<TextureButton>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/MenuButton");
		menuButton.TextureNormal = menuTexture;
		ImageTexture civilopediaTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 36, 1, 35, 29);
		TextureButton civilopediaButton = GetNode<TextureButton>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/CivilopediaButton");
		civilopediaButton.TextureNormal = civilopediaTexture;
		
		ImageTexture advisorsTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 73, 1, 35, 29);
		TextureButton advisorsButton = GetNode<TextureButton>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/AdvisorButton");
		advisorsButton.TextureNormal = advisorsTexture;
		advisorsButton.Connect("pressed", this, "ShowDomesticAdvisor");
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

	private void ShowDomesticAdvisor()
	{
		GD.Print("User requested domestic advisor");
		if (AdvisorContainer == null) {
			GD.Print("Creating and showing advisor");
			AdvisorContainer = GetNode<CenterContainer>("CanvasLayer/Advisor");
			DomesticAdvisor advisor = new DomesticAdvisor();
			AdvisorContainer.AddChild(advisor);

			//Center the advisor container.  Following directions at https://docs.godotengine.org/en/stable/tutorials/gui/size_and_anchors.html?highlight=anchor
			//Also taking advantage of it being 1024x768, as the directions didn't really work.  This is not 100% ideal (would be great for a general-purpose solution to work),
			//but does work with the current graphics independent of resolution.
			AdvisorContainer.MarginLeft = -512;
			AdvisorContainer.MarginRight = -512;
			AdvisorContainer.MarginTop = -384;
			AdvisorContainer.MarginBottom = 384;
		}
		else {
			GD.Print("Showing advisor");
			AdvisorContainer.Show();
		}
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

	private void OnHideAdvisor()
	{
		AdvisorContainer.Hide();
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
		MapView.Scale = NewScale;
		RefillMapView();
	}

	public void _OnViewportSizeChanged()
	{
		RefillMapView();
	}

	public void MoveCamera(Vector2 offset)
	{
		cameraLocation += offset;
		RefillMapView();
	}

	public void _on_RightButton_pressed()
	{
		MoveCamera(new Vector2(128, 0));
	}
	public void _on_LeftButton_pressed()
	{
		MoveCamera(new Vector2(-128, 0));
	}
	public void _on_UpButton_pressed()
	{
		MoveCamera(new Vector2(0, -64));
	}
	public void _on_DownButton_pressed()
	{
		MoveCamera(new Vector2(0, 64));
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
					IsMovingCamera = true;
				}
				else
				{
					IsMovingCamera = false;
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
			if(IsMovingCamera)
			{
				GetTree().SetInputAsHandled();
				MoveCamera((OldPosition - eventMouseMotion.Position) / Scale);
				OldPosition = eventMouseMotion.Position;
			}
		}
	}

}
