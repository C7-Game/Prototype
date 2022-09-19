using Godot;
using System;
using System.Collections;
using System.Diagnostics;
using C7Engine;
using C7GameData;
using Serilog;

public class Game : Node2D
{
	[Signal] public delegate void TurnStarted();
	[Signal] public delegate void TurnEnded();
	[Signal] public delegate void ShowSpecificAdvisor();
	[Signal] public delegate void NewAutoselectedUnit();
	[Signal] public delegate void NoMoreAutoselectableUnits();

	private ILogger log = LogManager.ForContext<Game>();

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
	private bool inUnitGoToMode = false;

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
	// The catch should always catch any error, as it's the general catch
	// that gives an error if we fail to load for some reason.
	public override void _Ready()
	{
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		try {
			var animSoundPlayer = new AudioStreamPlayer();
			AddChild(animSoundPlayer);
			civ3AnimData = new Civ3AnimData(animSoundPlayer);
			animTracker = new AnimationTracker(civ3AnimData);

			controller = CreateGame.createGame(Global.LoadGamePath, Global.DefaultBicPath); // Spawns engine thread
			Global.ResetLoadGamePath();

			using (var gameDataAccess = new UIGameDataAccess()) {
				GameMap map = gameDataAccess.gameData.map;
				Util.setModPath(gameDataAccess.gameData.scenarioSearchPath);
				log.Debug("RelativeModPath ", map.RelativeModPath);
				mapView = new MapView(this, map.numTilesWide, map.numTilesTall, map.wrapHorizontally, map.wrapVertically);
				AddChild(mapView);

				mapView.cameraZoom = (float)1.0;
				mapView.gridLayer.visible = false;

				// Set initial camera location. If the UI controller has any cities, focus on their capital. Otherwise, focus on their
				// starting settler.
				if (controller.cities.Count > 0) {
					City capital = controller.cities.Find(c => c.IsCapital());
					if (capital != null)
						mapView.centerCameraOnTile(capital.location);
				} else {
					MapUnit startingSettler = controller.units.Find(u => u.unitType.actions.Contains("buildCity"));
					if (startingSettler != null)
						mapView.centerCameraOnTile(startingSettler.location);
				}
			}

			Toolbar = GetNode<Control>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer");
			Player = GetNode<KinematicBody2D>("KinematicBody2D");
			//TODO: What was this supposed to do?  It throws errors and occasinally causes crashes now, because _OnViewportSizeChanged doesn't exist
			// GetTree().Root.Connect("size_changed", this, "_OnViewportSizeChanged");

			// Hide slideout bar on startup
			_on_SlideToggle_toggled(false);

			log.Information("Now in game!");

			loadTimer.Stop();
			TimeSpan stopwatchElapsed = loadTimer.Elapsed;
			log.Information("Game scene load time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");
		}
		catch(Exception ex) {
			errorOnLoad = true;
			PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
			string message = ex.Message;
			string[] stack = ex.StackTrace.Split("\r\n");	//for some reason it is returned with \r\n in the string as one line.  let's make it readable!
			foreach (string line in stack) {
				message = message + "\r\n" + line;
			}

			popupOverlay.ShowPopup(new ErrorMessage(message), PopupOverlay.PopupCategory.Advisor);
			log.Error(ex, "Unexpected error in Game.cs _Ready");
		}
	}

	// Must only be called while holding the game data mutex
	public void processEngineMessages(GameData gameData)
	{
		MessageToUI msg;
		while (EngineStorage.messagesToUI.TryDequeue(out msg)) {
			switch (msg) {
			case MsgStartUnitAnimation mSUA:
				MapUnit unit = gameData.GetUnit(mSUA.unitGUID);
				if (unit != null && (controller.tileKnowledge.isTileKnown(unit.location) || controller.tileKnowledge.isTileKnown(unit.previousLocation))) {
					// TODO: This needs to be extended so that the player is shown when AIs found cities, when they move units
					// (optionally, depending on preferences) and generalized so that modders can specify whether custom
					// animations should be shown to the player.
					if (mSUA.action == MapUnit.AnimatedAction.ATTACK1)
						ensureLocationIsInView(unit.location);

					animTracker.startAnimation(unit, mSUA.action, mSUA.completionEvent, mSUA.ending);
				} else {
					if (mSUA.completionEvent != null) {
						mSUA.completionEvent.Set();
					}
				}
				break;
			case MsgStartEffectAnimation mSEA:
				int x, y;
				gameData.map.tileIndexToCoords(mSEA.tileIndex, out x, out y);
				Tile tile = gameData.map.tileAt(x, y);
				if (tile != Tile.NONE && controller.tileKnowledge.isTileKnown(tile))
					animTracker.startAnimation(tile, mSEA.effect, mSEA.completionEvent, mSEA.ending);
				else {
					if (mSEA.completionEvent != null)
						mSEA.completionEvent.Set();
				}
				break;
			case MsgStartTurn mST:
				OnPlayerStartTurn();
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
				if (CurrentState == GameState.PlayerTurn) {
					// If the selected unit is unfortified, prepare to autoselect the next one if it becomes fortified
					if ((CurrentlySelectedUnit != MapUnit.NONE) && (! CurrentlySelectedUnit.isFortified))
						KeepCSUWhenFortified = false;

					// Advance off the currently selected unit to the next one if it's out of moves or HP and not playing an
					// animation we want to watch, or if it's fortified and we aren't set to keep fortified units selected.
					if ((CurrentlySelectedUnit != MapUnit.NONE) &&
						(((!CurrentlySelectedUnit.movementPoints.canMove || CurrentlySelectedUnit.hitPointsRemaining <= 0) &&
						  ! animTracker.getUnitAppearance(CurrentlySelectedUnit).DeservesPlayerAttention()) ||
						 (CurrentlySelectedUnit.isFortified && ! KeepCSUWhenFortified)))
						GetNextAutoselectedUnit(gameData);
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
		if (controller.tileKnowledge.isTileKnown(location) && location != Tile.NONE) {
			Vector2 relativeScreenLocation = mapView.screenLocationOfTile(location, true) / mapView.getVisibleAreaSize();
			if (relativeScreenLocation.DistanceTo(new Vector2((float)0.5, (float)0.5)) > 0.30)
				mapView.centerCameraOnTile(location);
		}
	}

	public void SetAnimationsEnabled(bool enabled)
	{
		new MsgSetAnimationsEnabled(enabled).send();
		animTracker.endAllImmediately = ! enabled;
	}

	/**
	 * Currently (11/14/2021), all unit selection goes through here.
	 * Both code paths are in Game.cs for now, so it's local, but we may
	 * want to change it event driven.
	 **/
	public void setSelectedUnit(MapUnit unit)
	{
		unit = UnitInteractions.UnitWithAvailableActions(unit);

		if ((unit.path?.PathLength() ?? -1) > 0) {
			log.Debug("cancelling path for " + unit);
			unit.path = TilePath.NONE;
		}

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
			log.Information("It's not your turn!");
		}
	}

	private void OnPlayerStartTurn()
	{
		log.Information("Starting player turn");
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
			log.Information("Ending player turn");
			EmitSignal(nameof(TurnEnded));
			log.Information("Starting computer turn");
			CurrentState = GameState.ComputerTurn;
			new MsgEndTurn().send(); // Triggers actual backend processing
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
					if (inUnitGoToMode) {
						setGoToMode(false);
						using (var gameDataAccess = new UIGameDataAccess()) {
							var tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
							if (tile != null) {
								new MsgSetUnitPath(CurrentlySelectedUnit.guid, tile).send();
							}
						}
					}
					else
					{
						// Select unit on tile at mouse location
						using (var gameDataAccess = new UIGameDataAccess()) {
							var tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
							if (tile != null) {
								MapUnit to_select = tile.unitsOnTile.Find(u => u.movementPoints.canMove);
								if (to_select != null && to_select.owner == controller)
									setSelectedUnit(to_select);
							}
						}

						OldPosition = eventMouseButton.Position;
						IsMovingCamera = true;
					}
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
			else if ((eventMouseButton.ButtonIndex == (int)ButtonList.Right) && (!eventMouseButton.IsPressed()))
			{
				setGoToMode(false);
				using (var gameDataAccess = new UIGameDataAccess()) {
					var tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
					if (tile != null) {
						bool shiftDown = Input.IsKeyPressed((int)Godot.KeyList.Shift);
						if (shiftDown && tile.cityAtTile?.owner == controller)
							new RightClickChooseProductionMenu(this, tile.cityAtTile).Open(eventMouseButton.Position);
						else if ((! shiftDown) && tile.unitsOnTile.Count > 0)
							new RightClickTileMenu(this, tile).Open(eventMouseButton.Position);

						string yield = tile.YieldString(controller);
						//These GD.Print statements are debugging prints for developers to see info about the tile
						//For now I'm leaving them as GD.Print.  Could revisit this later.
						GD.Print($"({tile.xCoordinate}, {tile.yCoordinate}): {tile.overlayTerrainType.DisplayName} {yield}");

						if (tile.cityAtTile != null) {
							City city = tile.cityAtTile;
							GD.Print($"  {city.name}, production {city.shieldsStored} of {city.itemBeingProduced.shieldCost}");
							foreach (CityResident resident in city.residents) {
								GD.Print($"  Resident working at {resident.tileWorked}");
							}
						}

						if (tile.unitsOnTile.Count > 0) {
							foreach (MapUnit unit in tile.unitsOnTile) {
								GD.Print("  Unit on tile: " + unit);
								GD.Print("  Strategy: " + unit.currentAIData);
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
		else if (@event is InputEventKey eventKeyDown && eventKeyDown.Pressed)
		{
			if (eventKeyDown.Scancode == (int)Godot.KeyList.Enter)
			{
				log.Verbose("Enter pressed");
				if (CurrentlySelectedUnit == MapUnit.NONE)
				{
					this.OnPlayerEndTurn();
				}
				else {
					log.Debug("There is a " + CurrentlySelectedUnit.unitType.name + " selected; not ending turn");
				}
			}
			else if (eventKeyDown.Scancode == (int)Godot.KeyList.Space)
			{
				log.Verbose("Space pressed");
				if (CurrentlySelectedUnit == MapUnit.NONE)
				{
					this.OnPlayerEndTurn();
				}
			}
			else if ((eventKeyDown.Scancode >= (int)Godot.KeyList.Kp1) && (eventKeyDown.Scancode <= (int)Godot.KeyList.Kp9))
			{ // Move units with the numpad keys
				if (CurrentlySelectedUnit != MapUnit.NONE)
				{
					TileDirection dir;
					switch (eventKeyDown.Scancode - (int)Godot.KeyList.Kp0) {
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
					new MsgMoveUnit(CurrentlySelectedUnit.guid, dir).send();
					setSelectedUnit(CurrentlySelectedUnit);	//also triggers updating the lower-left info box
				}
			}
			else if ((eventKeyDown.Scancode >= (int)Godot.KeyList.Home) && (eventKeyDown.Scancode <= (int)Godot.KeyList.Pagedown))
			{ // Move units with the arrow and fn keys
				if (CurrentlySelectedUnit != MapUnit.NONE)
				{
					TileDirection dir;
					switch (eventKeyDown.Scancode) {
					case (int)Godot.KeyList.Home:     dir = TileDirection.NORTHWEST; break; // fn-left arrow
					case (int)Godot.KeyList.End:      dir = TileDirection.SOUTHWEST; break; // fn-right arrow
					case (int)Godot.KeyList.Left:     dir = TileDirection.WEST;      break;
					case (int)Godot.KeyList.Up:       dir = TileDirection.NORTH;     break;
					case (int)Godot.KeyList.Right:    dir = TileDirection.EAST;      break;
					case (int)Godot.KeyList.Down:     dir = TileDirection.SOUTH;     break;
					case (int)Godot.KeyList.Pageup:   dir = TileDirection.NORTHEAST; break; // fn-up arrow
					case (int)Godot.KeyList.Pagedown: dir = TileDirection.SOUTHEAST; break; // fn-down arrow
					default: return; // Impossible
					}
					new MsgMoveUnit(CurrentlySelectedUnit.guid, dir).send();
					setSelectedUnit(CurrentlySelectedUnit);	//also triggers updating the lower-left info box
				}
			}
			else if (eventKeyDown.Scancode == (int)Godot.KeyList.G && eventKeyDown.Control)
			{
				mapView.gridLayer.visible = !mapView.gridLayer.visible;
			}
			else if (eventKeyDown.Scancode == (int)Godot.KeyList.Escape)
			{
				if (!inUnitGoToMode) {
					log.Debug("Got request for escape/quit");
					PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
					popupOverlay.ShowPopup(new EscapeQuitPopup(), PopupOverlay.PopupCategory.Info);
				}
			}
			else if (eventKeyDown.Scancode == (int)Godot.KeyList.Z)
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
			else if (eventKeyDown.Scancode == (int)Godot.KeyList.Shift && ! eventKeyDown.Echo)
			{
				SetAnimationsEnabled(false);
			}

			// always turn off go to mode unless G key is pressed
			// do this after processing esc key
			setGoToMode(eventKeyDown.Scancode == (int)Godot.KeyList.G);
		}
		else if (@event is InputEventKey eventKeyUp && ! eventKeyUp.Pressed)
		{
			if (eventKeyUp.Scancode == (int)Godot.KeyList.Shift)
			{
				SetAnimationsEnabled(true);
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

	private void setGoToMode(bool isOn)
	{
		inUnitGoToMode = isOn;
	}

	///This is our global handler for unit buttons being pressed.  Both the mouse clicks and
	///the keyboard shortcuts should wind up here.
	///Eventually, we should quite possibly put this somewhere other than Game.cs, or at
	///least the logic should be somewhere else.  I want to see how it looks with a couple
	///more things going on before figuring out what the 'right' thing is, though.
	private void UnitButtonPressed(string buttonName)
	{
		// this will detoggle goTo when clicking unit buttons
		// other than goTo
		setGoToMode(buttonName == "goTo");

		log.Verbose("The " + buttonName + " button was pressed");
		switch (buttonName) {
			case "hold":
				new MsgSkipUnitTurn(CurrentlySelectedUnit.guid).send();
				break;

			case "fortify":
				new MsgSetFortification(CurrentlySelectedUnit.guid, true).send();
				break;

			case "wait":
				using (var gameDataAccess = new UIGameDataAccess()) {
					UnitInteractions.waitUnit(gameDataAccess.gameData, CurrentlySelectedUnit.guid);
					GetNextAutoselectedUnit(gameDataAccess.gameData);
				}
				break;

			case "disband":
			{
				PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
				popupOverlay.ShowPopup(new DisbandConfirmation(CurrentlySelectedUnit), PopupOverlay.PopupCategory.Advisor);
			}
				break;

			case "buildCity": {
				using (var gameDataAccess = new UIGameDataAccess()) {
					MapUnit currentUnit = gameDataAccess.gameData.GetUnit(CurrentlySelectedUnit.guid);
					if (currentUnit.canBuildCity()) {
						PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
						popupOverlay.ShowPopup(new BuildCityDialog(controller.GetNextCityName()),
							PopupOverlay.PopupCategory.Advisor);
					}
				}
			}
				break;

			case "buildRoad": {
				if (CurrentlySelectedUnit.canBuildRoad()) {
					new MsgBuildRoad(CurrentlySelectedUnit.guid).send();
				}
			}
				break;

			case "goTo":
				break;

			default:
				//A nice sanity check if I use a different name here than where I created it...
				log.Warning("An unrecognized button " + buttonName + " was pressed");
				break;
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
		new MsgDisbandUnit(CurrentlySelectedUnit.guid).send();
	}

	/**
	 * User quit.  We *may* want to do some things here like make a back-up save, or call the server and let it know we're bailing (esp. in MP).
	 **/
	private void OnQuitTheGame()
	{
		log.Information("Goodbye!");
		GetTree().Quit();
	}

	private void OnBuildCity(string name)
	{
		new MsgBuildCity(CurrentlySelectedUnit.guid, name).send();
	}
}
