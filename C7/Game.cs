using Godot;
using System;
using System.Diagnostics;
using C7Engine;
using C7GameData;
using Serilog;
using C7Engine.Pathing;
using System.Collections.Generic;

public partial class Game : Node2D {
	[Signal] public delegate void TurnStartedEventHandler();
	[Signal] public delegate void TurnEndedEventHandler();
	[Signal] public delegate void ShowSpecificAdvisorEventHandler();
	[Signal] public delegate void NewAutoselectedUnitEventHandler();
	[Signal] public delegate void NoMoreAutoselectableUnitsEventHandler();
	[Signal] public delegate void UpdateTechProgressEventHandler();
	[Signal] public delegate void ShowCityScreenEventHandler();

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

	GameState CurrentState = GameState.PreGame;

	// CurrentlySelectedUnit is a reference directly into the game state so be careful of race conditions. TODO: Consider storing a GUID instead.
	public MapUnit CurrentlySelectedUnit = MapUnit.NONE; //The selected unit.  May be changed by clicking on a unit or the next unit being auto-selected after orders are given for the current one.
	private bool HasCurrentlySelectedUnit() => CurrentlySelectedUnit != MapUnit.NONE;

	// When the game is in "goto" mode, the current destination and the cost of getting
	// there, in turns.
	//
	// Otherwise null.
	public class GotoInfo {
		public Tile destinationTile = null;
		public int moveCost = -1;
		public HashSet<System.Numerics.Vector2> pathCoords;
	};
	public GotoInfo gotoInfo = null;

	// Normally if the currently selected unit (CSU) becomes fortified, we advance to the next autoselected unit. If this flag is set, we won't do
	// that. This is useful so that the unit autoselector can be prevented from interfering with the player selecting fortified units.
	public bool KeepCSUWhenFortified = false;

	[Export]
	Control Toolbar;
	private bool IsMovingCamera;
	private Vector2 OldPosition;

	Stopwatch loadTimer = new Stopwatch();
	GlobalSingleton Global;

	[Export]
	private PopupOverlay popupOverlay;
	[Export]
	private CityScreen cityScreen;
	[Export]
	private Advisors advisor;
	[Export]
	private VSlider slider;
	[Export]
	private AnimationPlayer animationPlayer;

	bool errorOnLoad = false;

	public override void _EnterTree() {
		loadTimer.Start();
	}

	// Called when the node enters the scene tree for the first time.
	// The catch should always catch any error, as it's the general catch
	// that gives an error if we fail to load for some reason.
	public override void _Ready() {
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");

		try {
			var animSoundPlayer = new AudioStreamPlayer();
			AddChild(animSoundPlayer);
			civ3AnimData = new AnimationManager(animSoundPlayer);
			animTracker = new AnimationTracker(civ3AnimData);

			controller = CreateGame.createGame(Global.LoadGamePath, Global.DefaultBicPath, (scenarioSearchPath) => {
				// WHen the game loading logic tries to load the PediaIcons file, set the
				// scenario search path and then use our Civ3MediaPath searching logic to
				// find the correct version of the file.
				//
				// This weird bit of indirection is necessary because the C7GameData project
				// can't depend on the C7 project without a circular dependency, and the
				// search logic has a Godot dependency, so it doesn't make sense to live
				// in the C7GameData project.
				//
				// This also helps ensure the weird stateful behavior of the Util class works,
				// since the search path/mod path is a static global variable - we want to
				// be sure it is always set properly, so doing it during game creation
				// is reasonable.
				Util.setModPath(scenarioSearchPath);
				log.Debug("RelativeModPath ", scenarioSearchPath);
				return Util.Civ3MediaPath("Text/PediaIcons.txt");
			}); // Spawns engine thread
			Global.ResetLoadGamePath();

			using (var gameDataAccess = new UIGameDataAccess()) {
				GameMap map = gameDataAccess.gameData.map;
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
					MapUnit startingSettler = controller.units.Find(u => u.unitType.actions.Contains(C7Action.UnitBuildCity));
					if (startingSettler != null)
						mapView.centerCameraOnTile(startingSettler.location);
				}

				// Allow the city screen to control whether tile assignments
				// are visible and map UI locations back to map locations.
				cityScreen.tileAssignmentLayer = mapView.tileAssignmentLayer;
				cityScreen.mapView = mapView;
			}

			//TODO: What was this supposed to do?  It throws errors and occasinally causes crashes now, because _OnViewportSizeChanged doesn't exist
			// GetTree().Root.Connect("size_changed",new Callable(this,"_OnViewportSizeChanged"));

			// Hide slideout bar on startup
			_on_SlideToggle_toggled(false);

			log.Information("Now in game!");

			loadTimer.Stop();
			TimeSpan stopwatchElapsed = loadTimer.Elapsed;
			log.Information("Game scene load time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");
		} catch (Exception ex) {
			errorOnLoad = true;
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
					MapUnit unit = gameData.GetUnit(mSUA.unitID);
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
					int X, Y;
					gameData.map.tileIndexToCoords(mSEA.tileIndex, out X, out Y);
					Tile tile = gameData.map.tileAt(X, Y);
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
				case MsgCityCreated mCC:
					ShowCityScreenForCity(mCC.city);
					break;
				case MsgCityDestroyed mCD:
					mapView.cityLayer.UpdateAfterCityDestruction(mCD.city);

					// If this was the last city of the civilization, display a popup
					// noting that the civ is gone and destroy any remaining units.
					//
					// TODO: Implement the full set of conditions for destroying a civ;
					// handling cases like 1 city elimination, regicide, settlers that
					// are still alive, etc.
					if (mCD.city.owner.RemainingCities() == 0) {
						popupOverlay.ShowPopup(new CivilizationDestroyed(mCD.city.owner.civilization), PopupOverlay.PopupCategory.Advisor);
						for (int i = 0; i < mCD.city.owner.units.Count; ++i) {
							MapUnitExtensions.disband(mCD.city.owner.units[i]);
						}
					}
					break;
				case MsgUpdateUiAfterMove mUUAM:
					// The unit finished moving and still has moves left, so we need to
					// mark it as the selected unit again.
					//
					// Among other things, this will refresh the UI and ensure that the
					// unit action buttons are correct.
					if (CurrentlySelectedUnit != MapUnit.NONE) {
						setSelectedUnit(CurrentlySelectedUnit);
					}
					break;
				case MsgUpdateUiAfterTechSelection mUUATS:
					// F6 is the science advisor.
					// TODO: Move the F* key strings to a set of constants/enum.
					EmitSignal(SignalName.ShowSpecificAdvisor, "F6");
					Player player = gameData.GetHumanPlayers()[0];
					Tech tech = gameData.techs.Find(x => x.id == player.currentlyResearchedTech);

					if (tech != null) {
						EmitSignal(SignalName.UpdateTechProgress, tech.Name, player.EstimateTurnsToResearch(tech));
					} else {
						EmitSignal(SignalName.UpdateTechProgress, "Not selected", int.MaxValue);
					}
					break;
				case MsgUpdateUiAfterSliderChange mUUASC:
					// F1 is the science advisor.
					// TODO: Move the F* key strings to a set of constants/enum.
					EmitSignal(SignalName.ShowSpecificAdvisor, "F1");
					break;
			}
		}
	}

	// Instead of Game calling animTracker.update periodically (this used to happen in _Process), this method gets called as necessary to bring
	// the animations up to date. Right now it's called from UnitLayer right before it draws the units on the map. This method also processes all
	// waiting messages b/c some of them might pertain to animations. TODO: Consider processing only the animation messages here.
	// Must only be called while holding the game data mutex
	public void updateAnimations(GameData gameData) {
		processEngineMessages(gameData);
		animTracker.update();
	}

	public override void _Process(double delta) {
		ProcessActions();

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
				//Listen to keys.  There is a C# Mono Godot bug where e.g. Godot.Key.F1 (etc.) doesn't work
				//without a manual cast to int.
				//https://github.com/godotengine/godot/issues/16388
				if (Input.IsKeyPressed(Godot.Key.F1)) {
					EmitSignal(SignalName.ShowSpecificAdvisor, "F1");
				}
				if (Input.IsKeyPressed(Godot.Key.F6)) {
					EmitSignal(SignalName.ShowSpecificAdvisor, "F6");
				}
			}
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
		for (int Y = 0; Y < mapHeight; Y++) {
			for (int X = 0; X < mapWidth; X++) {
				// Multiplying X & Y for noise coordinate sampling
				float n = noise.GetNoise2D(X*2,Y*2);
				tr[X, Y] = n < 0.1 ? 2 : n < 0.4 ? 1 : 0;
			}
		}
		return tr;
	}

	// If "location" is not already near the center of the screen, moves the camera to bring it into view.
	public void ensureLocationIsInView(Tile location) {
		if (controller.tileKnowledge.isTileKnown(location) && location != Tile.NONE) {
			Vector2 relativeScreenLocation = mapView.screenLocationOfTile(location, true) / mapView.getVisibleAreaSize();
			if (relativeScreenLocation.DistanceTo(new Vector2((float)0.5, (float)0.5)) > 0.30)
				mapView.centerCameraOnTile(location);
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
		unit.availableActions = UnitInteractions.GetAvailableActions(unit);

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
			EmitSignal(SignalName.NewAutoselectedUnit, wrappedUnit);
		} else {
			EmitSignal(SignalName.NoMoreAutoselectableUnits);
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
		using (var gameDataAccess = new UIGameDataAccess()) {
			int turnNumber = TurnHandling.GetTurnNumber();
			Player player = gameDataAccess.gameData.GetHumanPlayers()[0];

			EmitSignal(SignalName.TurnStarted, turnNumber, player.gold, /*goldPerTurn=*/0);

			Tech tech = gameDataAccess.gameData.techs.Find(x => x.id == player.currentlyResearchedTech);
			if (tech != null) {
				EmitSignal(SignalName.UpdateTechProgress, tech.Name, player.EstimateTurnsToResearch(tech));
			} else {
				EmitSignal(SignalName.UpdateTechProgress, "Not selected", int.MaxValue);
			}
			CurrentState = GameState.PlayerTurn;

			GetNextAutoselectedUnit(gameDataAccess.gameData);
		}
	}

	private void OnPlayerEndTurn() {
		if (CurrentState == GameState.PlayerTurn) {
			log.Information("Ending player turn");
			EmitSignal(SignalName.TurnEnded);
			log.Information("Starting computer turn");
			CurrentState = GameState.ComputerTurn;
			new MsgEndTurn().send(); // Triggers actual backend processing
		}
	}

	public void _on_QuitButton_pressed() {
		// This apparently exits the whole program
		// GetTree().Quit();

		// ChangeSceneToFile deletes the current scene and frees its memory, so this is quitting to main menu
		GetTree().ChangeSceneToFile("res://MainMenu.tscn");
	}

	public void _on_Zoom_value_changed(float value) {
		mapView.setCameraZoomFromMiddle(value);
	}

	public void AdjustZoomSlider(int numSteps, Vector2 zoomCenter) {
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

	public void _on_RightButton_pressed() {
		mapView.cameraLocation += new Vector2(128, 0);
	}
	public void _on_LeftButton_pressed() {
		mapView.cameraLocation += new Vector2(-128, 0);
	}
	public void _on_UpButton_pressed() {
		mapView.cameraLocation += new Vector2(0, -64);
	}
	public void _on_DownButton_pressed() {
		mapView.cameraLocation += new Vector2(0, 64);
	}

	public override void _Input(InputEvent @event) {
		if (@event is InputEventKey e && e.Pressed && !e.IsAction(C7Action.UnitGoto)) {
			this.setGotoMode(false);
		}
	}

	public override void _UnhandledInput(InputEvent @event) {
		// Control node must not be in the way and/or have mouse pass enabled
		if (@event is InputEventMouseButton eventMouseButton) {
			if (eventMouseButton.ButtonIndex == MouseButton.Left) {
				GetViewport().SetInputAsHandled();
				if (eventMouseButton.IsPressed()) {
					if (gotoInfo != null) {
						setGotoMode(false);
						using (var gameDataAccess = new UIGameDataAccess()) {
							var tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
							if (tile != null) {
								new MsgSetUnitPath(CurrentlySelectedUnit.id, tile).send();
							}
						}
					} else {
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
				} else {
					IsMovingCamera = false;
				}
			} else if (eventMouseButton.ButtonIndex == MouseButton.WheelUp) {
				GetViewport().SetInputAsHandled();
				AdjustZoomSlider(1, GetViewport().GetMousePosition());
			} else if (eventMouseButton.ButtonIndex == MouseButton.WheelDown) {
				GetViewport().SetInputAsHandled();
				AdjustZoomSlider(-1, GetViewport().GetMousePosition());
			} else if ((eventMouseButton.ButtonIndex == MouseButton.Right) && (!eventMouseButton.IsPressed())) {
				setGotoMode(false);
				using (var gameDataAccess = new UIGameDataAccess()) {
					var tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
					if (tile != null) {
						bool shiftDown = Input.IsKeyPressed(Godot.Key.Shift);

						// Handle the shortcut of shift+right clicking a city to get the change production menu.
						if (shiftDown && tile.cityAtTile?.owner == controller)
							new RightClickChooseProductionMenu(this, tile.cityAtTile).Open(eventMouseButton.Position);
						else if ((!shiftDown) && tile.unitsOnTile.Count > 0)
							// There are units on this title, so open that menu.
							new RightClickTileMenu(this, tile).Open(eventMouseButton.Position);
						else if ((!shiftDown) && tile.cityAtTile?.owner == controller)
							// There are no units, but this is the player's city.
							new RightClickCityMenu(this, tile).Open(eventMouseButton.Position);

						string yield = tile.YieldString(controller);
						log.Debug($"({tile.XCoordinate}, {tile.YCoordinate}): {tile.overlayTerrainType.DisplayName} {yield}");

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
			if (IsMovingCamera) {
				GetViewport().SetInputAsHandled();
				mapView.cameraLocation += OldPosition - eventMouseMotion.Position;
				OldPosition = eventMouseMotion.Position;
			} else if (gotoInfo != null) {
				// We're in "goto" mode and moved the mouse over a tile.
				//
				// Figure out which tile it was.
				using UIGameDataAccess gameDataAccess = new();
				Tile tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseMotion.Position);
				gotoInfo.destinationTile = tile;

				// Figure out what unit is in goto mode. If the tile we're hovering over is
				// different than the tile the unit is on, calculate the path to move there.
				MapUnit unit = tile == null ? null : gameDataAccess.gameData.GetUnit(CurrentlySelectedUnit.id);
				if (unit != null && unit.location != tile) {
					TilePath path = PathingAlgorithmChooser.GetAlgorithm(unit.IsLandUnit()).PathFrom(unit.location, tile);
					gotoInfo.moveCost = path.PathCost(unit.location, unit.unitType.movement, unit.movementPoints.remaining);
					gotoInfo.pathCoords = path.GetPathCoords();
				} else {
					// Hide the goto cursor, we don't have a valid move.
					gotoInfo.moveCost = -1;
				}
			}
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
							if (player.id == EngineStorage.uiControllerID) {
								player.isHuman = true;
							}
						}
					}
				}
			}
		} else if (@event is InputEventMagnifyGesture magnifyGesture) {
			// UI slider has the min/max zoom settings for now
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

	// Handle Godot keybind actions
	private void ProcessActions() {
		Godot.Collections.Array<StringName> actions = InputMap.GetActions();

		foreach (StringName action in actions) {
			if (Input.IsActionJustPressed(action)) {
				ProcessAction(action.ToString());
			}
		}
	}

	private void ProcessAction(string currentAction) {
		if (currentAction == C7Action.Escape && popupOverlay.ShowingPopup) {
			popupOverlay.OnHidePopup();
			return;
		}

		if (currentAction == C7Action.Escape && cityScreen.Visible) {
			cityScreen.HideScreen();
			return;
		}

		if (currentAction == C7Action.Escape && advisor.Visible) {
			advisor.Hide();
			return;
		}

		// never poll for actions if UI elements are visible
		if (popupOverlay.Visible || cityScreen.Visible || advisor.Visible) {
			return;
		}

		if (currentAction == C7Action.EndTurn && !this.HasCurrentlySelectedUnit()) {
			log.Verbose("end_turn key pressed");
			this.OnPlayerEndTurn();
		}

		if (this.HasCurrentlySelectedUnit()) {
			TileDirection? dir = C7Action.ToTileDirection(currentAction);

			if (dir.HasValue) {
				new MsgMoveUnit(CurrentlySelectedUnit.id, dir.Value).send();
			}
		}

		if (currentAction == C7Action.ToggleGrid) {
			this.mapView.gridLayer.visible = !this.mapView.gridLayer.visible;
		}

		if (currentAction == C7Action.Escape && this.gotoInfo == null) {
			log.Debug("Got request for escape/quit");
			popupOverlay.ShowPopup(new EscapeQuitPopup(), PopupOverlay.PopupCategory.Info);
		}

		if (currentAction == C7Action.ToggleZoom) {
			if (mapView.cameraZoom != 1) {
				mapView.setCameraZoomFromMiddle(1.0f);
				slider.Value = 1.0f;
			} else {
				mapView.setCameraZoomFromMiddle(0.5f);
				slider.Value = 0.5f;
			}
		}

		if (currentAction == C7Action.ToggleAnimations) {
			SetAnimationsEnabled(false);
		} else if (Input.IsActionJustReleased(C7Action.ToggleAnimations)) {
			SetAnimationsEnabled(true);
		}

		// actions with unit buttons
		if (currentAction == C7Action.UnitHold) {
			new ActionToEngineMsg(() => CurrentlySelectedUnit?.skipTurn()).send();
		}

		if (currentAction == C7Action.UnitWait) {
			using (var gameDataAccess = new UIGameDataAccess()) {
				UnitInteractions.waitUnit(gameDataAccess.gameData, CurrentlySelectedUnit.id);
				GetNextAutoselectedUnit(gameDataAccess.gameData);
			}
		}

		if (currentAction == C7Action.UnitFortify) {
			new MsgSetFortification(CurrentlySelectedUnit.id, true).send();
		}

		if (currentAction == C7Action.UnitDisband) {
			popupOverlay.ShowPopup(new DisbandConfirmation(CurrentlySelectedUnit), PopupOverlay.PopupCategory.Advisor);
		}

		// unit_goto's behavior is more complicated than other actions - it
		// toggles the go to state, but must be detoggled in _*Input methods if
		// it is not the input being pressed.
		if (currentAction == C7Action.UnitGoto) {
			setGotoMode(true);
		}

		if (currentAction == C7Action.UnitExplore) {
			// unimplemented
		}

		if (currentAction == C7Action.UnitSentry) {
			// unimplemented
		}

		if (currentAction == C7Action.UnitSentryEnemyOnly) {
			// unimplemented
		}

		if (currentAction == C7Action.UnitBuildCity && CurrentlySelectedUnit.canBuildCity()) {
			using (var gameDataAccess = new UIGameDataAccess()) {
				MapUnit currentUnit = gameDataAccess.gameData.GetUnit(CurrentlySelectedUnit.id);
				log.Debug(currentUnit.Describe());
				if (currentUnit.canBuildCity()) {
					popupOverlay.ShowPopup(new BuildCityDialog(controller.GetNextCityName()), PopupOverlay.PopupCategory.Advisor);
				}
			}
		}

		if (currentAction == C7Action.UnitBuildRoad && CurrentlySelectedUnit.canBuildRoad()) {
			new ActionToEngineMsg(() => CurrentlySelectedUnit?.buildRoad()).send();
		}

		if (currentAction == C7Action.UnitBuildMine && CurrentlySelectedUnit.canBuildMine()) {
			new ActionToEngineMsg(() => CurrentlySelectedUnit?.buildMine()).send();
		}

		if (currentAction == C7Action.UnitIrrigate && CurrentlySelectedUnit.canIrrigate()) {
			new ActionToEngineMsg(() => CurrentlySelectedUnit?.irrigate()).send();
		}

	}

	private void GetNextAutoselectedUnit(GameData gameData) {
		this.setSelectedUnit(UnitInteractions.getNextSelectedUnit(gameData));
	}

	private void setGotoMode(bool isOn) {
		if (isOn) {
			gotoInfo = new();
		} else {
			gotoInfo = null;
		}
	}

	private void _on_SlideToggle_toggled(bool buttonPressed) {
		if (buttonPressed) {
			animationPlayer.PlayBackwards("SlideOutAnimation");
		} else {
			animationPlayer.Play("SlideOutAnimation");
		}
	}

	// Called by the disband popup
	private void OnUnitDisbanded() {
		new ActionToEngineMsg(() => CurrentlySelectedUnit?.disband()).send();
	}

	/**
	 * User quit.  We *may* want to do some things here like make a back-up save, or call the server and let it know we're bailing (esp. in MP).
	 **/
	private void OnQuitTheGame() {
		log.Information("Goodbye!");
		GetTree().Quit();
	}

	private void OnBuildCity(string name) {
		new ActionToEngineMsg(() => {
			// Create the city and then let the ui know, so we can show the city
			// screen.
			City? city = CurrentlySelectedUnit?.buildCity(name);
			if (city != null) {
				new MsgCityCreated(city).send();
			}
		}).send();
	}

	public void ShowCityScreenForCity(City city) {
		EmitSignal(SignalName.ShowCityScreen, new ParameterWrapper<City>(city));
	}
}
