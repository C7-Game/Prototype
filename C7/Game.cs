using Godot;
using System;
using System.Collections;
using System.Diagnostics;
using C7Engine;
using C7GameData;
using Serilog;
using C7.Map;

public partial class Game : Node2D {
	[Signal] public delegate void TurnStartedEventHandler();
	[Signal] public delegate void TurnEndedEventHandler();
	[Signal] public delegate void ShowSpecificAdvisorEventHandler();
	[Signal] public delegate void NewAutoselectedUnitEventHandler();
	[Signal] public delegate void NoMoreAutoselectableUnitsEventHandler();

	private ILogger log = LogManager.ForContext<Game>();

	enum GameState {
		PreGame,
		PlayerTurn,
		ComputerTurn
	}

	public Player controller; // Player that's controlling the UI.
	private MapView mapView;
	public AnimationManager civ3AnimData;
	public AnimationTracker animTracker;

	Hashtable Terrmask = new Hashtable();
	GameState CurrentState = GameState.PreGame;

	// CurrentlySelectedUnit is a reference directly into the game state so be careful of race conditions. TODO: Consider storing a GUID instead.
	public MapUnit CurrentlySelectedUnit = MapUnit.NONE; //The selected unit.  May be changed by clicking on a unit or the next unit being auto-selected after orders are given for the current one.
	private bool HasCurrentlySelectedUnit() => CurrentlySelectedUnit != MapUnit.NONE;

	private bool inUnitGoToMode = false;

	// Normally if the currently selected unit (CSU) becomes fortified, we advance to the next autoselected unit. If this flag is set, we won't do
	// that. This is useful so that the unit autoselector can be prevented from interfering with the player selecting fortified units.
	public bool KeepCSUWhenFortified = false;

	Control Toolbar;

	Stopwatch loadTimer = new Stopwatch();
	GlobalSingleton Global;
	public MapViewCamera camera;
	bool errorOnLoad = false;

	public override void _EnterTree() {
		loadTimer.Start();
	}

	// Called when the node enters the scene tree for the first time.
	// The catch should always catch any error, as it's the general catch
	// that gives an error if we fail to load for some reason.
	public override void _Ready() {
		GetTree().AutoAcceptQuit = false;
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		try {
			var animSoundPlayer = new AudioStreamPlayer();
			AddChild(animSoundPlayer);
			civ3AnimData = new AnimationManager(animSoundPlayer);
			animTracker = new AnimationTracker(civ3AnimData);

			controller = CreateGame.createGame(Global.LoadGamePath, Global.DefaultBicPath); // Spawns engine thread
			Global.ResetLoadGamePath();
			camera = GetNode<MapViewCamera>("MapViewCamera");

			using (var gameDataAccess = new UIGameDataAccess()) {
				GameMap map = gameDataAccess.gameData.map;
				Util.setModPath(gameDataAccess.gameData.scenarioSearchPath);
				log.Debug("RelativeModPath ", map.RelativeModPath);

				mapView = new MapView(this, gameDataAccess.gameData);

				// Set initial camera location. If the UI controller has any cities, focus on their capital. Otherwise, focus on their
				// starting settler.
				if (controller.cities.Count > 0) {
					City capital = controller.cities.Find(c => c.IsCapital());
					if (capital is not null) {
						camera.centerOnTile(capital.location, mapView);
					}
				} else {
					MapUnit startingSettler = controller.units.Find(u => u.unitType.actions.Contains(C7Action.UnitBuildCity));
					if (startingSettler is not null) {
						camera.centerOnTile(startingSettler.location, mapView);
					}
				}
				camera.attachToMapView(mapView);
				AddChild(mapView);
			}

			Toolbar = GetNode<Control>("CanvasLayer/Control/ToolBar/MarginContainer/HBoxContainer");

			// Hide slideout bar on startup
			_on_SlideToggle_toggled(false);

			log.Information("Now in game!");

			loadTimer.Stop();
			TimeSpan stopwatchElapsed = loadTimer.Elapsed;
			log.Information("Game scene load time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");
		} catch (Exception ex) {
			errorOnLoad = true;
			PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
			string message = ex.Message;
			string[] stack = ex.StackTrace.Split("\r\n");   //for some reason it is returned with \r\n in the string as one line.  let's make it readable!
			foreach (string line in stack) {
				message = message + "\r\n" + line;
			}

			popupOverlay.ShowPopup(new ErrorMessage(message), PopupOverlay.PopupCategory.Advisor);
			log.Error(ex, "Unexpected error in Game.cs _Ready");
		}
	}

	// Must only be called while holding the game data mutex
	public void processEngineMessages(GameData gameData) {
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
					Tile tile = gameData.map.tileAtIndex(mSEA.tileIndex);
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

				case MsgCityBuilt mBC:
					Tile cityTile = gameData.map.tiles[mBC.tileIndex];
					City city = cityTile.cityAtTile;
					mapView.addCity(city, cityTile);
					break;

				case MsgTileDiscovered mTD:
					Tile discoveredTile = gameData.map.tileAtIndex(mTD.tileIndex);
					mapView.discoverTile(discoveredTile, controller.tileKnowledge);
					break;
			}
		}
	}

	// updateAnimations updates animation states in the tracker and then their corresponding
	// sprites in the MapView. It must be called when holding the game data mutex.
	// TODO: before switching to tilemap, this was only called by the old UnitLayer _Draw
	// method. It only really needs to be called as frequently as animations update...
	public void updateAnimations(GameData gameData) {
		animTracker.update();
		mapView.updateAnimations();
	}

	public override void _Process(double delta) {
		processActions();

		// TODO: Is it necessary to keep the game data mutex locked for this entire method?
		using (var gameDataAccess = new UIGameDataAccess()) {
			GameData gameData = gameDataAccess.gameData;

			processEngineMessages(gameData);

			if (!errorOnLoad) {
				if (CurrentState == GameState.PlayerTurn) {
					// If the selected unit is unfortified, prepare to autoselect the next one if it becomes fortified
					if ((CurrentlySelectedUnit != MapUnit.NONE) && (!CurrentlySelectedUnit.isFortified))
						KeepCSUWhenFortified = false;

					// Advance off the currently selected unit to the next one if it's out of moves or HP and not playing an
					// animation we want to watch, or if it's fortified and we aren't set to keep fortified units selected.
					if ((CurrentlySelectedUnit != MapUnit.NONE) &&
						(((!CurrentlySelectedUnit.movementPoints.canMove || CurrentlySelectedUnit.hitPointsRemaining <= 0) &&
						  !animTracker.getUnitAppearance(CurrentlySelectedUnit).DeservesPlayerAttention()) ||
						 (CurrentlySelectedUnit.isFortified && !KeepCSUWhenFortified)))
						GetNextAutoselectedUnit(gameData);
				}
				// Listen to keys. TODO: move this
				if (Input.IsKeyPressed(Godot.Key.F1)) {
					EmitSignal("ShowSpecificAdvisor", "F1");
				}
			}

			updateAnimations(gameDataAccess.gameData);
		}
	}

	// This is the terrain generator that used to be part of TerrainAsTileMap. Now it gets passed to and called from generateDummyGameMap so that
	// function can be more in charge of terrain generation. Eventually we'll want generation to be part of the engine not the UI but we can't
	// simply move this function there right now since we don't want the engine to depend on Godot.
	public int[,] genBasicTerrainNoiseMap(int seed, int mapWidth, int mapHeight) {
		var tr = new int[mapWidth,mapHeight];
		Godot.FastNoiseLite noise = new Godot.FastNoiseLite();
		noise.Seed = seed;
		// Populate map values
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				// Multiplying x & y for noise coordinate sampling
				float n = noise.GetNoise2D(x*2,y*2);
				tr[x, y] = n < 0.1 ? 2 : n < 0.4 ? 1 : 0;
			}
		}
		return tr;
	}

	// If "location" is not already near the center of the screen, moves the camera to bring it into view.
	public void ensureLocationIsInView(Tile location) {
		if (controller.tileKnowledge.isTileKnown(location) && location != Tile.NONE) {
			if (!camera.isTileInView(location, mapView)) {
				camera.centerOnTile(location, mapView);
			}
		}
	}

	public void SetAnimationsEnabled(bool enabled) {
		new MsgSetAnimationsEnabled(enabled).send();
		animTracker.endAllImmediately = !enabled;
	}

	/**
	 * Currently (11/14/2021), all unit selection goes through here.
	 * Both code paths are in Game.cs for now, so it's local, but we may
	 * want to change it event driven.
	 **/
	public void setSelectedUnit(MapUnit unit) {
		unit = UnitInteractions.UnitWithAvailableActions(unit);

		if ((unit.path?.PathLength() ?? -1) > 0) {
			log.Debug("cancelling path for " + unit);
			unit.path = TilePath.NONE;
		}

		this.CurrentlySelectedUnit = unit;
		this.KeepCSUWhenFortified = unit.isFortified; // If fortified, make sure the autoselector doesn't immediately skip past the unit

		if (unit != MapUnit.NONE) {
			ensureLocationIsInView(unit.location);
		}

		// Also emit the signal for a new unit being selected, so other areas such as Game Status and Unit Buttons can update
		if (CurrentlySelectedUnit != MapUnit.NONE) {
			ParameterWrapper<MapUnit> wrappedUnit = new ParameterWrapper<MapUnit>(CurrentlySelectedUnit);
			EmitSignal("NewAutoselectedUnit", wrappedUnit);
		} else {
			EmitSignal("NoMoreAutoselectableUnits");
		}
	}

	private void _onEndTurnButtonPressed() {
		if (CurrentState == GameState.PlayerTurn) {
			OnPlayerEndTurn();
		} else {
			log.Information("It's not your turn!");
		}
	}

	private void OnPlayerStartTurn() {
		log.Information("Starting player turn");
		int turnNumber = TurnHandling.GetTurnNumber();
		EmitSignal("TurnStarted", turnNumber);
		CurrentState = GameState.PlayerTurn;

		using (var gameDataAccess = new UIGameDataAccess()) {
			GetNextAutoselectedUnit(gameDataAccess.gameData);
		}
	}

	private void OnPlayerEndTurn() {
		if (CurrentState == GameState.PlayerTurn) {
			log.Information("Ending player turn");
			EmitSignal("TurnEnded");
			log.Information("Starting computer turn");
			CurrentState = GameState.ComputerTurn;
			new MsgEndTurn().send(); // Triggers actual backend processing
		}
	}

	public void _on_QuitButton_pressed() {
		// ChangeSceneToFile deletes the current scene and frees its memory, so this is quitting to main menu
		GetTree().ChangeSceneToFile("res://MainMenu.tscn");
	}

	public void AdjustZoomSlider(int numSteps, Vector2 zoomCenter) {
		VSlider slider = GetNode<VSlider>("CanvasLayer/Control/SlideOutBar/VBoxContainer/Zoom");
		double newScale = slider.Value + slider.Step * (double)numSteps;
		newScale = Mathf.Clamp(newScale, slider.MinValue, slider.MaxValue);

		// Note we must set the camera zoom before setting the new slider value since setting the value will trigger the callback which will
		// adjust the zoom around a center we don't want.
		// camera.scaleZoom(zoom)
		slider.Value = newScale;
	}

	private void onSliderZoomChanged(float value) {
		camera.setZoom(value);
	}

	public override void _Input(InputEvent @event) {
		if (@event is InputEventKey e && e.Pressed && !e.IsAction(C7Action.UnitGoto)) {
			this.setGoToMode(false);
		}
	}

	public override void _UnhandledInput(InputEvent @event) {
		// Control node must not be in the way and/or have mouse pass enabled
		if (@event is InputEventMouseButton eventMouseButton) {
			Vector2 globalMousePosition = GetGlobalMousePosition();
			if (eventMouseButton.ButtonIndex == MouseButton.Left) {
				GetViewport().SetInputAsHandled();
				if (eventMouseButton.IsPressed()) {
					if (inUnitGoToMode) {
						setGoToMode(false);
						using (var gameDataAccess = new UIGameDataAccess()) {
							var tile = mapView.tileAt(gameDataAccess.gameData.map, globalMousePosition);
							if (tile != null) {
								new MsgSetUnitPath(CurrentlySelectedUnit.guid, tile).send();
							}
						}
					} else {
						// Select unit on tile at mouse location
						using (var gameDataAccess = new UIGameDataAccess()) {
							var tile = mapView.tileAt(gameDataAccess.gameData.map, globalMousePosition);
							if (tile != null) {
								MapUnit to_select = tile.unitsOnTile.Find(u => u.movementPoints.canMove);
								if (to_select != null && to_select.owner == controller)
									setSelectedUnit(to_select);
							}
						}
					}
				}
			} else if (eventMouseButton.ButtonIndex == MouseButton.WheelUp) {
				GetViewport().SetInputAsHandled();
				AdjustZoomSlider(1, GetViewport().GetMousePosition());
			} else if (eventMouseButton.ButtonIndex == MouseButton.WheelDown) {
				GetViewport().SetInputAsHandled();
				AdjustZoomSlider(-1, GetViewport().GetMousePosition());
			} else if (eventMouseButton.ButtonIndex == MouseButton.Right && !eventMouseButton.IsPressed()) {
				setGoToMode(false);
				using (var gameDataAccess = new UIGameDataAccess()) {
					var tile = mapView.tileAt(gameDataAccess.gameData.map, globalMousePosition);
					if (tile != null) {
						bool shiftDown = Input.IsKeyPressed(Godot.Key.Shift);
						if (shiftDown && tile.cityAtTile?.owner == controller)
							new RightClickChooseProductionMenu(this, tile.cityAtTile).Open(eventMouseButton.Position);
						else if ((!shiftDown) && tile.unitsOnTile.Count > 0)
							new RightClickTileMenu(this, tile).Open(eventMouseButton.Position);

						string yield = tile.YieldString(controller);
						log.Debug($"({tile.xCoordinate}, {tile.yCoordinate}): {tile.overlayTerrainType.DisplayName} {yield}");

						if (tile.cityAtTile != null) {
							City city = tile.cityAtTile;
							log.Debug($"  {city.name}, production {city.shieldsStored} of {city.itemBeingProduced.shieldCost}");
							foreach (CityResident resident in city.residents) {
								log.Debug($"  Resident working at {resident.tileWorked}");
							}
						}

						if (tile.unitsOnTile.Count > 0) {
							foreach (MapUnit unit in tile.unitsOnTile) {
								log.Debug("  Unit on tile: " + unit);
								log.Debug("  Strategy: " + unit.currentAIData);
							}
						}
					} else {
						log.Debug("Didn't click on any tile");
					}
				}
			}
		} else if (@event is InputEventMouseMotion eventMouseMotion) {
			GetViewport().SetInputAsHandled();
		} else if (@event is InputEventKey eventKeyDown && eventKeyDown.Pressed) {
			if (eventKeyDown.Keycode == Godot.Key.O && eventKeyDown.ShiftPressed && eventKeyDown.IsCommandOrControlPressed() && eventKeyDown.AltPressed) {
				using (UIGameDataAccess gameDataAccess = new UIGameDataAccess()) {
					gameDataAccess.gameData.observerMode = !gameDataAccess.gameData.observerMode;
					if (gameDataAccess.gameData.observerMode) {
						foreach (Player player in gameDataAccess.gameData.players) {
							player.isHuman = false;
						}
					} else {
						foreach (Player player in gameDataAccess.gameData.players) {
							if (player.guid == EngineStorage.uiControllerID) {
								player.isHuman = true;
							}
						}
					}
				}
			}
		}
	}

	// Handle Godot keybind actions
	private void processActions() {

		if (Input.IsActionJustPressed(C7Action.EndTurn) && !this.HasCurrentlySelectedUnit()) {
			log.Verbose("end_turn key pressed");
			this.OnPlayerEndTurn();
		}

		if (this.HasCurrentlySelectedUnit()) {
			// TODO: replace bool with an invalid TileDirection enum
			TileDirection dir = TileDirection.NORTH;
			bool moveUnit = true;
			if (Input.IsActionJustPressed(C7Action.MoveUnitSouthwest)) {
				dir = TileDirection.SOUTHWEST;
			} else if (Input.IsActionJustPressed(C7Action.MoveUnitSouth)) {
				dir = TileDirection.SOUTH;
			} else if (Input.IsActionJustPressed(C7Action.MoveUnitSoutheast)) {
				dir = TileDirection.SOUTHEAST;
			} else if (Input.IsActionJustPressed(C7Action.MoveUnitWest)) {
				dir = TileDirection.WEST;
			} else if (Input.IsActionJustPressed(C7Action.MoveUnitEast)) {
				dir = TileDirection.EAST;
			} else if (Input.IsActionJustPressed(C7Action.MoveUnitNorthwest)) {
				dir = TileDirection.NORTHWEST;
			} else if (Input.IsActionJustPressed(C7Action.MoveUnitNorth)) {
				dir = TileDirection.NORTH;
			} else if (Input.IsActionJustPressed(C7Action.MoveUnitNortheast)) {
				dir = TileDirection.NORTHEAST;
			} else {
				moveUnit = false;
			}
			if (moveUnit) {
				new MsgMoveUnit(CurrentlySelectedUnit.guid, dir).send();
				setSelectedUnit(CurrentlySelectedUnit); //also triggers updating the lower-left info box
			}
		}

		if (Input.IsActionJustPressed(C7Action.ToggleGrid)) {
			mapView.toggleGrid();
		}

		if (Input.IsActionJustPressed(C7Action.Escape) && !this.inUnitGoToMode) {
			log.Debug("Got request for escape/quit");
			PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
			popupOverlay.ShowPopup(new EscapeQuitPopup(), PopupOverlay.PopupCategory.Info);
		}

		if (Input.IsActionJustPressed(C7Action.ToggleZoom)) {
			camera.setZoom(camera.zoomFactor != 1 ? 1.0f : 0.5f);
		}

		if (Input.IsActionJustPressed(C7Action.ToggleAnimations)) {
			SetAnimationsEnabled(false);
		} else if (Input.IsActionJustReleased(C7Action.ToggleAnimations)) {
			SetAnimationsEnabled(true);
		}

		// actions with unit buttons
		if (Input.IsActionJustPressed(C7Action.UnitHold)) {
			new MsgSkipUnitTurn(CurrentlySelectedUnit.guid).send();
		}

		if (Input.IsActionJustPressed(C7Action.UnitWait)) {
			using (var gameDataAccess = new UIGameDataAccess()) {
				UnitInteractions.waitUnit(gameDataAccess.gameData, CurrentlySelectedUnit.guid);
				GetNextAutoselectedUnit(gameDataAccess.gameData);
			}
		}

		if (Input.IsActionJustPressed(C7Action.UnitFortify)) {
			new MsgSetFortification(CurrentlySelectedUnit.guid, true).send();
		}

		if (Input.IsActionJustPressed(C7Action.UnitDisband)) {
			PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
			popupOverlay.ShowPopup(new DisbandConfirmation(CurrentlySelectedUnit), PopupOverlay.PopupCategory.Advisor);
		}

		// unit_goto's behavior is more complicated than other actions - it
		// toggles the go to state, but must be detoggled in _*Input methods if
		// it is not the input being pressed.
		if (Input.IsActionJustPressed(C7Action.UnitGoto)) {
			setGoToMode(true);
		}

		if (Input.IsActionJustPressed(C7Action.UnitExplore)) {
			// unimplemented
		}

		if (Input.IsActionJustPressed(C7Action.UnitSentry)) {
			// unimplemented
		}

		if (Input.IsActionJustPressed(C7Action.UnitSentryEnemyOnly)) {
			// unimplemented
		}

		if (Input.IsActionJustPressed(C7Action.UnitBuildCity) && CurrentlySelectedUnit.canBuildCity()) {
			using (var gameDataAccess = new UIGameDataAccess()) {
				MapUnit currentUnit = gameDataAccess.gameData.GetUnit(CurrentlySelectedUnit.guid);
				log.Debug(currentUnit.Describe());
				if (currentUnit.canBuildCity()) {
					PopupOverlay popupOverlay = GetNode<PopupOverlay>(PopupOverlay.NodePath);
					popupOverlay.ShowPopup(new BuildCityDialog(controller.GetNextCityName()),
						PopupOverlay.PopupCategory.Advisor);
				}
			}
		}

		if (Input.IsActionJustPressed(C7Action.UnitBuildRoad) && CurrentlySelectedUnit.canBuildRoad()) {
			new MsgBuildRoad(CurrentlySelectedUnit.guid).send();
		}
	}

	private void GetNextAutoselectedUnit(GameData gameData) {
		this.setSelectedUnit(UnitInteractions.getNextSelectedUnit(gameData));
	}

	private void setGoToMode(bool isOn) {
		inUnitGoToMode = isOn;
	}

	private void _on_SlideToggle_toggled(bool buttonPressed) {
		if (buttonPressed) {
			GetNode<AnimationPlayer>("CanvasLayer/Control/SlideOutBar/AnimationPlayer").PlayBackwards("SlideOutAnimation");
		} else {
			GetNode<AnimationPlayer>("CanvasLayer/Control/SlideOutBar/AnimationPlayer").Play("SlideOutAnimation");
		}
	}

	// Called by the disband popup
	private void OnUnitDisbanded() {
		new MsgDisbandUnit(CurrentlySelectedUnit.guid).send();
	}

	/**
	 * User quit. We *may* want to do some things here like make a back-up save, or call the server and let it know we're bailing (esp. in MP).
	 **/
	public override void _Notification(int what) {
		if (what == NotificationWMCloseRequest) {
			log.Information("Goodbye!");
			GetTree().Quit();
		}
	}

	private void OnBuildCity(string name) {
		new MsgBuildCity(CurrentlySelectedUnit.guid, name).send();
	}
}
