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

	public Player controller; // Player that's controlling the UI.

	private MapView mapView;
	public Civ3AnimData civ3AnimData;
	public AnimationTracker animTracker;

	Hashtable Terrmask = new Hashtable();
	GameState CurrentState = GameState.PreGame;

	// CurrentlySelectedUnit is a reference directly into the game state so be careful of race conditions. TODO: Consider storing a GUID instead.
	public MapUnit CurrentlySelectedUnit = MapUnit.NONE;	//The selected unit.  May be changed by clicking on a unit or the next unit being auto-selected after orders are given for the current one.

	// Normally if the currently selected unit (CSU) becomes fortified, we advance to the next autoselected unit. If this flag is set, we won't do
	// that. This is useful so that the unit autoselector can be prevented from interfering with the player selecting fortified units.
	public bool KeepCSUWhenFortified = false;

	Control Toolbar;
	private bool IsMovingCamera;
	private Vector2 OldPosition;
	private KinematicBody2D Player;

	Stopwatch loadTimer = new Stopwatch();
	GlobalSingleton Global;

	bool errorOnLoad = false;

	public override void _EnterTree()
	{
		loadTimer.Start();
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		try {
			var animSoundPlayer = new AudioStreamPlayer();
			AddChild(animSoundPlayer);
			civ3AnimData = new Civ3AnimData(animSoundPlayer);
			animTracker = new AnimationTracker(civ3AnimData);
			EngineStorage.initialize(); // Spawns engine thread

			controller = CreateGame.createGame(Global.LoadGamePath, Global.DefaultBicPath);
			Global.ResetLoadGamePath();

			using (var gameDataAccess = new UIGameDataAccess()) {
				GameMap map = gameDataAccess.gameData.map;
				GD.Print("RelativeModPath ", map.RelativeModPath);
				mapView = new MapView(this, map.numTilesWide, map.numTilesTall, map.wrapHorizontally, map.wrapVertically);
				AddChild(mapView);
			}

			Toolbar = GetNode<Control>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer");
			Player = GetNode<KinematicBody2D>("KinematicBody2D");
			//TODO: What was this supposed to do?  It throws errors and occasinally causes crashes now, because _OnViewportSizeChanged doesn't exist
			// GetTree().Root.Connect("size_changed", this, "_OnViewportSizeChanged");
			mapView.cameraZoom = (float)0.3;
			// If later recreating scene, the component may already exist, hence try/catch
			try{
				ComponentManager.Instance.AddComponent(new TurnCounterComponent());
			}
			catch {
				ComponentManager.Instance.GetComponent<TurnCounterComponent>().SetTurnCounter();
			}
		}
		catch(Exception ex) {
			errorOnLoad = true;
			PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
			popupOverlay.ShowPopup(new ErrorMessage(ex.Message), PopupOverlay.PopupCategory.Advisor);
			GD.PrintErr(ex);
		}

		// Hide slideout bar on startup
		_on_SlideToggle_toggled(false);

		GD.Print("Now in game!");

		loadTimer.Stop();
		TimeSpan stopwatchElapsed = loadTimer.Elapsed;
		GD.Print("Game scene load time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");
	}

	// Must only be called while holding the game data mutex
	public void processEngineMessages(GameData gameData)
	{
		MessageToUI msg;
		while (EngineStorage.messagesToUI.TryDequeue(out msg)) {
			switch (msg) {
			case MsgStartUnitAnimation mSUA:
				MapUnit unit = gameData.mapUnits.Find(u => u.guid == mSUA.unitGUID);
				if (unit != null) {
					// TODO: This needs to be extended so that the player is shown when AIs found cities, when they move units
					// (optionally, depending on preferences) and generalized so that modders can specify whether custom
					// animations should be shown to the player.
					if (mSUA.action == MapUnit.AnimatedAction.ATTACK1)
						ensureLocationIsInView(unit.location);

					animTracker.startAnimation(unit, mSUA.action, mSUA.completionEvent);
				}
				break;
			case MsgStartEffectAnimation mSEA:
				int x, y;
				gameData.map.tileIndexToCoords(mSEA.tileIndex, out x, out y);
				Tile tile = gameData.map.tileAt(x, y);
				if (tile != Tile.NONE)
					animTracker.startAnimation(tile, mSEA.effect, mSEA.completionEvent);
				break;
			case MsgStartTurn mST:
				//Simulating processing so the turn doesn't end too quickly
				ComputerSimulateTurn();
				GD.Print("Thinking...");
				break;
			}
		}
	}

	// Instead of Game calling animTracker.update periodically (this used to happen in _Process), this method gets called as necessary to bring
	// the animations up to date. Right now it's called from UnitLayer right before it draws the units on the map. This method also processes all
	// waiting messages b/c some of them might pertain to animations. TODO: Consider processing only the animation messages here.
	// Must only be called while holding the game data mutex
	public void updateAnimations(GameData gameData)
	{
		processEngineMessages(gameData);
		animTracker.update();
	}

	public override void _Process(float delta)
	{
		// TODO: Is it necessary to keep the game data mutex locked for this entire method?
		using (var gameDataAccess = new UIGameDataAccess()) {
			GameData gameData = gameDataAccess.gameData;

			processEngineMessages(gameData);

			if (!errorOnLoad) {
				switch (CurrentState)
				{
				case GameState.PreGame:
					StartGame();
					break;
				case GameState.PlayerTurn:
					// If the selected unit is unfortified, prepare to autoselect the next one if it becomes fortified
					if ((CurrentlySelectedUnit != MapUnit.NONE) && (! CurrentlySelectedUnit.isFortified))
						KeepCSUWhenFortified = false;

					// Advance off the currently selected unit to the next one if it's out of moves or HP and not playing an
					// animation we want to watch, or if it's fortified and we aren't set to keep fortified units selected.
					if ((CurrentlySelectedUnit != MapUnit.NONE) &&
						(((CurrentlySelectedUnit.movementPointsRemaining <= 0 || CurrentlySelectedUnit.hitPointsRemaining <= 0) &&
						  ! animTracker.getUnitAppearance(CurrentlySelectedUnit).DeservesPlayerAttention()) ||
						 (CurrentlySelectedUnit.isFortified && ! KeepCSUWhenFortified)))
						GetNextAutoselectedUnit(gameData);
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

	// If "location" is not already near the center of the screen, moves the camera to bring it into view.
	public void ensureLocationIsInView(Tile location)
	{
		if (location != Tile.NONE) {
			var relativeScreenLocation = mapView.screenLocationOfTile(location, true) / mapView.getVisibleAreaSize();
			if (relativeScreenLocation.DistanceTo(new Vector2((float)0.5, (float)0.5)) > 0.30)
				mapView.centerCameraOnTile(location);
		}
	}

	/**
	 * Currently (11/14/2021), all unit selection goes through here.
	 * Both code paths are in Game.cs for now, so it's local, but we may
	 * want to change it event driven.
	 **/
	public void setSelectedUnit(MapUnit unit)
	{
		this.CurrentlySelectedUnit = unit;
		this.KeepCSUWhenFortified = unit.isFortified; // If fortified, make sure the autoselector doesn't immediately skip past the unit

		if (unit != MapUnit.NONE)
			ensureLocationIsInView(unit.location);

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

		using (var gameDataAccess = new UIGameDataAccess()) {
			GetNextAutoselectedUnit(gameDataAccess.gameData);
		}
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
		new MsgEndTurn().send();
	}

	public async void ComputerSimulateTurn()
	{
		await ToSignal(GetTree().CreateTimer(0.25f), "timeout");
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
					using (var gameDataAccess = new UIGameDataAccess()) {
						var tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
						if (tile != null) {
							MapUnit to_select = tile.unitsOnTile.Find(u => u.movementPointsRemaining > 0);
							//TODO: Better check for "current/human player"
							if (to_select != null && to_select.owner.color == 0x4040FFFF)
								setSelectedUnit(to_select);
						}
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
				using (var gameDataAccess = new UIGameDataAccess()) {
					var tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
					if (tile != null) {
						if (tile.unitsOnTile.Count > 0)
							RightClickMenu.OpenForTile(this, eventMouseButton.Position, tile);

						GD.Print("Clicked on (" + tile.xCoordinate.ToString() + ", " + tile.yCoordinate.ToString() + "): " + tile.overlayTerrainType.name);
						if (tile.unitsOnTile.Count > 0) {
							foreach (MapUnit unit in tile.unitsOnTile) {
								GD.Print("  Unit on tile: " + unit);
								GD.Print("  Strategy: " + unit.currentAIBehavior);
							}
						}
					} else
						GD.Print("Didn't click on any tile");
				}
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
					TileDirection dir;
					switch (eventKey.Scancode - (int)Godot.KeyList.Kp0) {
					case 1: dir = TileDirection.SOUTHWEST; break;
					case 2: dir = TileDirection.SOUTH;     break;
					case 3: dir = TileDirection.SOUTHEAST; break;
					case 4: dir = TileDirection.WEST;      break;
					case 5: return; // Key pad 5 => don't move
					case 6: dir = TileDirection.EAST;      break;
					case 7: dir = TileDirection.NORTHWEST; break;
					case 8: dir = TileDirection.NORTH;     break;
					case 9: dir = TileDirection.NORTHEAST; break;
					default: return; // Impossible
					}
					UnitInteractions.moveUnit(CurrentlySelectedUnit.guid, dir);
				}
			}
			// I key toggles the grid. This should be CTRL+G to match the original game but that key combination gets intercepted by the
			// unit action handler.
			else if (eventKey.Scancode == (int)Godot.KeyList.I)
			{
				mapView.gridLayer.visible = ! mapView.gridLayer.visible;
			}
			else if (eventKey.Scancode == (int)Godot.KeyList.Escape)
			{
				GD.Print("Got request for escape/quit");
				PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
				popupOverlay.ShowPopup(new EscapeQuitPopup(), PopupOverlay.PopupCategory.Info);
			}
			else if (eventKey.Scancode == (int)Godot.KeyList.Z)
			{
				if (mapView.cameraZoom != 1) {
					mapView.setCameraZoomFromMiddle(1.0f);
					VSlider slider = GetNode<VSlider>("CanvasLayer/SlideOutBar/VBoxContainer/Zoom");
					slider.Value = 1.0f;
				}
				else {
					mapView.setCameraZoomFromMiddle(0.5f);
					VSlider slider = GetNode<VSlider>("CanvasLayer/SlideOutBar/VBoxContainer/Zoom");
					slider.Value = 0.5f;
				}
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

	private void GetNextAutoselectedUnit(GameData gameData)
	{
		this.setSelectedUnit(UnitInteractions.getNextSelectedUnit(gameData));
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
		}
		else if (buttonName.Equals("fortify"))
		{
			UnitInteractions.fortifyUnit(CurrentlySelectedUnit.guid);
		}
		else if (buttonName.Equals("wait"))
		{
			using (var gameDataAccess = new UIGameDataAccess()) {
				UnitInteractions.waitUnit(gameDataAccess.gameData, CurrentlySelectedUnit.guid);
			}
		}
		else if (buttonName.Equals("disband"))
		{
			PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
			popupOverlay.ShowPopup(new DisbandConfirmation(CurrentlySelectedUnit), PopupOverlay.PopupCategory.Advisor);
		}
		else if (buttonName.Equals("buildCity"))
		{
			PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
			popupOverlay.ShowPopup(new BuildCityDialog(controller.GetNextCityName()), PopupOverlay.PopupCategory.Advisor);
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
	
	// Called by the disband popup
	private void OnUnitDisbanded()
	{
		UnitInteractions.disbandUnit(CurrentlySelectedUnit.guid);
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
		new MsgBuildCity(CurrentlySelectedUnit.guid, name).send();
	}
}
