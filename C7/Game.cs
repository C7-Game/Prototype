using Godot;
using System;
using System.Collections;
using System.Diagnostics;
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

	Player controller; // Player that's controlling the UI.

	private MapView mapView;

	Hashtable Terrmask = new Hashtable();
	GameState CurrentState = GameState.PreGame;
	public MapUnit CurrentlySelectedUnit = MapUnit.NONE;	//The selected unit.  May be changed by clicking on a unit or the next unit being auto-selected after orders are given for the current one.
	Control Toolbar;
	private bool IsMovingCamera;
	private Vector2 OldPosition;
	private KinematicBody2D Player;

	Stopwatch loadTimer = new Stopwatch();

	public override void _EnterTree()
	{
		loadTimer.Start();
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		controller = CreateGame.createGame(genBasicTerrainNoiseMap);
		var map = MapInteractions.GetWholeMap();
		Civ3Map baseTerrainMap = new Civ3Map(map.numTilesWide, map.numTilesTall);
		baseTerrainMap.Civ3Tiles = map.tiles;
		baseTerrainMap.TerrainAsTileMap();

		mapView = new MapView(this, map.numTilesWide, map.numTilesTall, false, false);
		AddChild(mapView);

		Toolbar = GetNode<Control>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer");
		Player = GetNode<KinematicBody2D>("KinematicBody2D");
		GetTree().Root.Connect("size_changed", this, "_OnViewportSizeChanged");
		mapView.cameraZoom = (float)0.3;
		// If later recreating scene, the component may already exist, hence try/catch
		try{
			ComponentManager.Instance.AddComponent(new TurnCounterComponent());
		}
		catch {
			ComponentManager.Instance.GetComponent<TurnCounterComponent>().SetTurnCounter();
		}

		// Hide slideout bar on startup
		_on_SlideToggle_toggled(false);

		GD.Print("Now in game!");

		loadTimer.Stop();
		TimeSpan stopwatchElapsed = loadTimer.Elapsed;
		GD.Print("Game scene load time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");
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
		if (Input.IsKeyPressed((int)Godot.KeyList.F1)) {
			EmitSignal("ShowSpecificAdvisor", "F1");
		}
	}

	private void StartGame()
	{
		GD.Print("Game starting");
		TurnCounterComponent turnCntCpnt = ComponentManager.Instance.GetComponent<TurnCounterComponent>();
		// Connect(nameof(TurnStarted), turnCntCpnt, "OnTurnStarted");
		// Connect(nameof(TurnEnded), this, nameof(OnPlayerEndTurn));
		OnPlayerStartTurn();
	}

	// This is the terrain generator that used to be part of TerrainAsTileMap. Now it gets passed to and called from generateDummyGameMap so that
	// function can be more in charge of terrain generation. Eventually we'll want generation to be part of the engine not the UI but we can't
	// simply move this function there right now since we don't want the engine to depend on Godot.
	public int[,] genBasicTerrainNoiseMap(int seed, int mapWidth, int mapHeight)
	{
		var tr = new int[mapWidth,mapHeight];
		Godot.OpenSimplexNoise noise = new Godot.OpenSimplexNoise();
		noise.Seed = seed;
		// Populate map values
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				// Multiplying x & y for noise coordinate sampling
				float foo = noise.GetNoise2d(x*2,y*2);
				tr[x,y] = foo < 0.1 ? 2 : foo < 0.4? 1 : 0;
			}
		}
		return tr;
	}

	/**
	 * Currently (11/14/2021), all unit selection goes through here.
	 * Both code paths are in Game.cs for now, so it's local, but we may
	 * want to change it event driven.
	 **/
	public void setSelectedUnit(MapUnit unit)
	{
		this.CurrentlySelectedUnit = unit;

		// If the newly selected unit's tile is not close to the center of the screen, then move the camera to it. Otherwise just trigger a
		// redraw (not necessary in the first case since moving the map triggers a redraw automatically).
		if (unit != MapUnit.NONE) {
			var relativeScreenLocation = mapView.screenLocationOfTile(unit.location, true) / mapView.getVisibleAreaSize();
			if (relativeScreenLocation.DistanceTo(new Vector2((float)0.5, (float)0.5)) > 0.30)
				mapView.centerCameraOnTile(unit.location);
			else
				mapView.onVisibleAreaChanged();
		} else
			mapView.onVisibleAreaChanged();

		//Also emit the signal for a new unit being selected, so other areas such as Game Status and Unit Buttons can update
		if (CurrentlySelectedUnit != MapUnit.NONE) {
			ParameterWrapper wrappedUnit = new ParameterWrapper(CurrentlySelectedUnit);
			EmitSignal(nameof(NewAutoselectedUnit), wrappedUnit);
		}
		else {
			EmitSignal(nameof(NoMoreAutoselectableUnits));
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
		int turnNumber = TurnHandling.GetTurnNumber();
		EmitSignal(nameof(TurnStarted), turnNumber);
		CurrentState = GameState.PlayerTurn;

		GetNextAutoselectedUnit();
	}

	private void OnPlayerEndTurn()
	{
		if (CurrentState == GameState.PlayerTurn)
		{
			GD.Print("Ending player turn");
			EmitSignal(nameof(TurnEnded));
			OnComputerStartTurn();
		}
	}

	private void OnComputerStartTurn()
	{
		GD.Print("Starting computer turn");
		CurrentState = GameState.ComputerTurn;
		//Actual backend processing
		TurnHandling.EndTurn();
		//Simulating processing so the turn doesn't end too quickly
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
		mapView.onVisibleAreaChanged();
	}

	public void AdjustZoomSlider(int numSteps, Vector2 zoomCenter)
	{
		VSlider slider = GetNode<VSlider>("CanvasLayer/SlideOutBar/VBoxContainer/Zoom");
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
					// Select unit on tile at mouse location
					var tile = mapView.tileOnScreenAt(eventMouseButton.Position);
					if (tile != null) {
						MapUnit to_select = tile.unitsOnTile.Find(u => u.movementPointsRemaining > 0);
						//TODO: Better check for "current/human player"
						if (to_select != null && to_select.owner.color == 0x4040FFFF)
							setSelectedUnit(to_select);
					}

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
			else if ((eventMouseButton.ButtonIndex == (int)ButtonList.Right) && (! eventMouseButton.IsPressed()))
			{
				var tile = mapView.tileOnScreenAt(eventMouseButton.Position);
				if (tile != null) {
					var terrainTypeName = tile.terrainType.name;
					GD.Print("Clicked on (" + tile.xCoordinate.ToString() + ", " + tile.yCoordinate.ToString() + "): " + terrainTypeName);
				} else
					GD.Print("Didn't click on any tile");
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
		else if (@event is InputEventKey eventKey && eventKey.Pressed)
		{
			if (eventKey.Scancode == (int)Godot.KeyList.Enter)
			{
				GD.Print("Enter pressed");
				if (CurrentlySelectedUnit == MapUnit.NONE)
				{
					GD.Print("Turn ending");
					this.OnPlayerEndTurn();
				}
				else {
					GD.Print("There is a " + CurrentlySelectedUnit.unitType.name + " selected; not ending turn");
				}
			}
			else if (eventKey.Scancode == (int)Godot.KeyList.Space)
			{
				GD.Print("Space pressed");
				if (CurrentlySelectedUnit == MapUnit.NONE)
				{
					this.OnPlayerEndTurn();
				}
			}
			else if ((eventKey.Scancode >= (int)Godot.KeyList.Kp1) && (eventKey.Scancode <= (int)Godot.KeyList.Kp9))
			{
				if (CurrentlySelectedUnit != MapUnit.NONE)
				{
					var dirs = new int[] {5, 4, 3, 6, 0, 2, 7, 8, 1}; // SW, S, SE, W, ., E, NW, N, NE
					var dir = dirs[eventKey.Scancode - (int)Godot.KeyList.Kp1];
					UnitInteractions.moveUnit(CurrentlySelectedUnit.guid, dir);
					if (CurrentlySelectedUnit.movementPointsRemaining <= 0)
						GetNextAutoselectedUnit();
					else {
						setSelectedUnit(CurrentlySelectedUnit);
					}
					mapView.onVisibleAreaChanged();
				}
			}
			// I key toggles the grid. This should be CTRL+G to match the original game but that key combination gets intercepted by the
			// unit action handler.
			else if (eventKey.Scancode == (int)Godot.KeyList.I)
			{
				mapView.gridLayer.visible = ! mapView.gridLayer.visible;
				mapView.onVisibleAreaChanged(); // Trigger redraw
			}
			else if (eventKey.Scancode == (int)Godot.KeyList.Escape)
			{
				GD.Print("Got request for escape/quit");
				PopupOverlay popupOverlay = GetNode<PopupOverlay>("CanvasLayer/PopupOverlay");
				popupOverlay.ShowPopup("escapeQuit", PopupOverlay.PopupCategory.Info, BoxContainer.AlignMode.Center);
			}
		}
		else if (@event is InputEventMagnifyGesture magnifyGesture)
		{
			// UI slider has the min/max zoom settings for now
			VSlider slider = GetNode<VSlider>("CanvasLayer/SlideOutBar/VBoxContainer/Zoom");
			double newScale = mapView.cameraZoom * magnifyGesture.Factor;
			if (newScale < slider.MinValue)
				newScale = slider.MinValue;
			else if (newScale > slider.MaxValue)
				newScale = slider.MaxValue;
			mapView.setCameraZoom((float)newScale, magnifyGesture.Position);
			// Update the UI slider
			slider.Value = newScale;
		}

	}

	private void GetNextAutoselectedUnit()
	{
		this.setSelectedUnit(UnitInteractions.getNextSelectedUnit());
	}

	///This is our global handler for unit buttons being pressed.  Both the mouse clicks and
	///the keyboard shortcuts should wind up here.
	///Eventually, we should quite possibly put this somewhere other than Game.cs, or at
	///least the logic should be somewhere else.  I want to see how it looks with a couple
	///more things going on before figuring out what the 'right' thing is, though.
	private void UnitButtonPressed(string buttonName)
	{
		GD.Print("The " + buttonName + " button was pressed");
		if (buttonName.Equals("hold"))
		{
			UnitInteractions.holdUnit(CurrentlySelectedUnit.guid);
			GetNextAutoselectedUnit();
		}
		else if (buttonName.Equals("fortify"))
		{
			UnitInteractions.fortifyUnit(CurrentlySelectedUnit.guid);
			GetNextAutoselectedUnit();
		}
		else if (buttonName.Equals("wait"))
		{
			UnitInteractions.waitUnit(CurrentlySelectedUnit.guid);
			GetNextAutoselectedUnit();
		}
		else if (buttonName.Equals("disband"))
		{
			string[] args = {CurrentlySelectedUnit.unitType.name};
			PopupOverlay popupOverlay = GetNode<PopupOverlay>("CanvasLayer/PopupOverlay");
			popupOverlay.ShowPopup("disband", PopupOverlay.PopupCategory.Advisor, args);
		}
		else if (buttonName.Equals("buildCity"))
		{
			PopupOverlay popupOverlay = GetNode<PopupOverlay>("CanvasLayer/PopupOverlay");
			popupOverlay.ShowPopup("buildCity", PopupOverlay.PopupCategory.Advisor);
		}
		else
		{
			//A nice sanity check if I use a different name here than where I created it...
			GD.PrintErr("An unrecognized button " + buttonName + " was pressed");
		}
	}

	private void _on_SlideToggle_toggled(bool buttonPressed)
	{
		if (buttonPressed)
		{
			GetNode<AnimationPlayer>("CanvasLayer/SlideOutBar/AnimationPlayer").PlayBackwards("SlideOutAnimation");
		}
		else
		{
			GetNode<AnimationPlayer>("CanvasLayer/SlideOutBar/AnimationPlayer").Play("SlideOutAnimation");
		}
	}
	
	private void OnUnitDisbanded()
	{
		UnitInteractions.disbandUnit(CurrentlySelectedUnit.guid);
		GetNextAutoselectedUnit();
	}
	
	/**
	 * User quit.  We *may* want to do some things here like make a back-up save, or call the server and let it know we're bailing (esp. in MP).
	 **/
	private void OnQuitTheGame()
	{
		GD.Print("Goodbye!");
		GetTree().Quit();
	}
	
	private void OnBuildCity(string name)
	{
		GD.Print("The user decided to build the city of " + name);
		Tile theTile = this.CurrentlySelectedUnit.location;
		CityInteractions.BuildCity(theTile.xCoordinate, theTile.yCoordinate, name);

		//Also dismantle the unit.  For now, I am considering that equivalent to
		//disbanding.  Whether that makes sense long term, is debatable.
		//I am only calling the UnitInteractions behavior (rather than OnUnitDisbanded),
		//however, since the one here will likely play a sound someday.
		UnitInteractions.disbandUnit(CurrentlySelectedUnit.guid);
		GetNextAutoselectedUnit();
	}
}
