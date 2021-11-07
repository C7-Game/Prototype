using Godot;
using System;
using System.Collections;
using ConvertCiv3Media;
using C7Engine;
using C7GameData;

public class Game : Node2D
{
	[Signal] public delegate void TurnStarted();
	[Signal] public delegate void TurnEnded();
	[Signal] public delegate void ShowSpecificAdvisor();
	[Signal] public delegate void NewAutoselectedUnit();
	[Signal] public delegate void NoMoreAutoselectableUnits();
	
	enum GameState {
		PreGame,
		PlayerTurn,
		ComputerTurn
	}


	int[,] Map;

	private MapView mapView;

	Hashtable Terrmask = new Hashtable();
	GameState CurrentState = GameState.PreGame;
	MapUnit CurrentlySelectedUnit = null;	//The selected unit.  May be changed by clicking on a unit or the next unit being auto-selected after orders are given for the current one.
	Button EndTurnButton;
	Control Toolbar;
	private bool IsMovingCamera;
	private Vector2 OldPosition;
	private KinematicBody2D Player;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Toolbar = GetNode<Control>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer");
		Player = GetNode<KinematicBody2D>("KinematicBody2D");
		GetTree().Root.Connect("size_changed", this, "_OnViewportSizeChanged");
		this.TerrainAsTileMap();
		mapView.cameraZoom = (float)0.3;
		this.CreateUI();
		// If later recreating scene, the component may already exist, hence try/catch
		try{
			ComponentManager.Instance.AddComponent(new TurnCounterComponent());
		}
		catch {
			ComponentManager.Instance.GetComponent<TurnCounterComponent>().SetTurnCounter();
		}
		
		CreateGame.createGame();
		
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
		if (Input.IsKeyPressed((int)Godot.KeyList.Escape))	//escape.  TODO: aka KEY_ESCAPE, which is global in GDScript but which I can't figure out how to import here.
		{
			GD.Print("User pressed escape");
			//TODO: Display the "Oh No! Do you really want to quit?" menu
		}
		else if (Input.IsKeyPressed((int)Godot.KeyList.F1)) {
			EmitSignal("ShowSpecificAdvisor", "F1");
		}
	}

	private void StartGame()
	{
		GD.Print("Game starting");
		TurnCounterComponent turnCntCpnt = ComponentManager.Instance.GetComponent<TurnCounterComponent>();
		Connect(nameof(TurnStarted), turnCntCpnt, nameof(turnCntCpnt.OnTurnStarted));
		// Connect(nameof(TurnEnded), this, nameof(OnPlayerEndTurn));
		OnPlayerStartTurn();
	}

	public void TerrainAsTileMap() {
		int mapWidth = 80, mapHeight = 80;

		TileSet TS = new TileSet();

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
		Godot.OpenSimplexNoise noise = new Godot.OpenSimplexNoise();
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
			for (int x = y%2; x < mapWidth; x+=2) {
				int Top = y == 0 ? (Map[(x+1) % mapWidth,y]) : (Map[x,y-1]);
				int Bottom = y == mapHeight - 1 ? (Map[(x+1) % mapWidth,y]) : (Map[x,y+1]);
				string foo = 
					(Map[(x+1) % mapWidth,y]).ToString("D3") +
					Bottom.ToString("D3") +
					(Map[Mathf.Abs((x-1) % mapWidth),y]).ToString("D3") +
					Top.ToString("D3")
				;
				try {
				Map[x,y] = (int)Terrmask[foo];
				} catch { GD.Print(x + "," + y + " " + foo); }
			}
		}

		mapView = new MapView(Map, TS, false, false);
		AddChild(mapView);
	}

	private void CreateUI()
	{
		EndTurnButton = new Button();
		EndTurnButton.Text = "End Turn";
		EndTurnButton.SetPosition(new Vector2(250, 10));
		Toolbar.AddChild(EndTurnButton);
		Toolbar.MoveChild(EndTurnButton, 0);
		EndTurnButton.Connect("pressed", this, "_onEndTurnButtonPressed");
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

		GetNextAutoselectedUnit();
	}

	private void OnPlayerEndTurn()
	{
		if (CurrentState == GameState.PlayerTurn)
		{
			GD.Print("Ending player turn");
			EndTurnButton.Disabled = true;
			EmitSignal(nameof(TurnEnded));
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
		mapView.setCameraZoomFromMiddle(value);
	}

	public void _OnViewportSizeChanged()
	{
		mapView.resetVisibleTiles();
	}

	public void AdjustZoomSlider(int numSteps, Vector2 zoomCenter)
	{
		HSlider slider = GetNode<HSlider>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/Zoom");
		double newScale = slider.Value + slider.Step * (double)numSteps;
		if (newScale < slider.MinValue)
			newScale = slider.MinValue;
		else if (newScale > slider.MaxValue)
			newScale = slider.MaxValue;

		// Note we must set the camera zoom before setting the new slider value since setting the value will trigger the callback which will
		// adjust the zoom around a center we don't want.
		mapView.setCameraZoom((float)newScale, zoomCenter);
		slider.Value = newScale;
	}

	public void _on_RightButton_pressed()
	{
		mapView.cameraLocation += new Vector2(128, 0);
	}
	public void _on_LeftButton_pressed()
	{
		mapView.cameraLocation += new Vector2(-128, 0);
	}
	public void _on_UpButton_pressed()
	{
		mapView.cameraLocation += new Vector2(0, -64);
	}
	public void _on_DownButton_pressed()
	{
		mapView.cameraLocation += new Vector2(0, 64);
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
				AdjustZoomSlider(1, GetViewport().GetMousePosition());
			}
			else if(eventMouseButton.ButtonIndex == (int)ButtonList.WheelDown)
			{
				GetTree().SetInputAsHandled();
				AdjustZoomSlider(-1, GetViewport().GetMousePosition());
			}
			else if (eventMouseButton.ButtonIndex == (int)ButtonList.Right)
			{
				int x, y;
				mapView.tileAt(eventMouseButton.Position, out x, out y);
				if (mapView.isTileAt(x, y)) {
					GD.Print("setting terrain sprite at (" + x.ToString() + ", " + y.ToString() + ") to 0");
					Map[x, y] = 0;
					mapView.resetVisibleTiles();
				} else
					GD.Print("No tile at (" + x.ToString() + ", " + y.ToString() + ")");
			}
		}
		else if(@event is InputEventMouseMotion eventMouseMotion)
		{
			if(IsMovingCamera)
			{
				GetTree().SetInputAsHandled();
				mapView.cameraLocation += OldPosition - eventMouseMotion.Position;
				OldPosition = eventMouseMotion.Position;
			}
		}
	}

	private void GetNextAutoselectedUnit()
	{
		//Set the selected unit in the lower right, via an event
		//We can't send the whole map unit via signals (probably because it can't be serialized?),
		//so I'm sending the name for now, as a temporary workaround.
		MapUnit SelectedUnit = UnitInteractions.getNextSelectedUnit();

		if (SelectedUnit == MapUnit.NONE) {
			EmitSignal(nameof(NoMoreAutoselectableUnits));
		}
		else {
			this.CurrentlySelectedUnit = SelectedUnit;
			ParameterWrapper wrappedUnit = new ParameterWrapper(SelectedUnit);
			EmitSignal(nameof(NewAutoselectedUnit), wrappedUnit);
		}
	}

	private void UnitButtonPressed(string buttonName)
	{
		GD.Print("The " + buttonName + " button was pressed");
		if (buttonName.Equals("hold"))
		{
			UnitInteractions.holdUnit(CurrentlySelectedUnit.guid);
			GetNextAutoselectedUnit();
		}
	}
}
