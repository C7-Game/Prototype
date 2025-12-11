using Godot;
using System;
using System.Diagnostics;
using C7Engine;
using C7GameData;
using Serilog;
using C7Engine.Pathing;
using System.Collections.Generic;
using System.Linq;

public partial class Game : Node {
	[Signal] public delegate void TurnEndedEventHandler();
	[Signal] public delegate void ShowSpecificAdvisorEventHandler();
	[Signal] public delegate void ShowCityScreenEventHandler();

	[Signal] public delegate void PlayerTurnStartEventHandler();
	[Signal] public delegate void PlayerTurnEndEventHandler();
	[Signal] public delegate void GameInitializedEventHandler();

	[Signal] public delegate void UnitMovedEventHandler();

	private ILogger log = LogManager.ForContext<Game>();

	public enum GameState {
		PreGame,
		PlayerTurn,
		ComputerTurn
	}

	public Player controller; // Player that's controlling the UI.

	private MapView mapView;

	public GameState CurrentState { get; private set; } = GameState.PreGame;

	public MapUnit CurrentlySelectedUnit => unitSelector.CurrentlySelectedUnit;
	private bool HasCurrentlySelectedUnit() => CurrentlySelectedUnit != MapUnit.NONE;

	// When the game is in "goto" mode, the current destination and the cost of getting
	// there, in turns.
	//
	// Otherwise null.
	public class GotoInfo {
		public Tile destinationTile = null;
		public int moveCost = -1;
		public TilePath path = null;
		public HashSet<System.Numerics.Vector2> pathCoords;
		public bool attackingMove = false;
		public Player requiresWarDeclarationOnPlayer = null;
	};
	public GotoInfo gotoInfo = null;

	// When in observer mode, the number of turns to play before prompting the
	// user to advance the turn manually. This allows for more rapid debugging
	// without pressing the spacebar repeatedly.
	public int turnsLeftToFastForward = 0;

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
	private Diplomacy diplomacy;
	[Export]
	private Control palaceScene;

	[Export]
	private DoubleClickHandler doubleClickHandler;
	[Export]
	public AnimationController animationController;
	[Export]
	public UnitSelector unitSelector;

	bool errorOnLoad = false;

	public override void _EnterTree() {
		loadTimer.Start();
	}

	// Called when the node enters the scene tree for the first time.
	// The catch should always catch any error, as it's the general catch
	// that gives an error if we fail to load for some reason.
	public override async void _Ready() {
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");

		try {
			// Ensure we clear out our image caches, as scenarios and games will
			// use the same filenames but have different content for them.
			Util.ClearCaches();

			controller = await CreateGame.createGame(
				Global.LoadGamePath,
				GamePaths.LuaRulesDir,
				GamePaths.DefaultBicPath,
				(scenarioSearchPath) => {
					// When the game loading logic tries to load the PediaIcons file, set the
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

			InitializeMapView();

			log.Information("Now in game!");

			loadTimer.Stop();
			TimeSpan stopwatchElapsed = loadTimer.Elapsed;
			log.Information("Game scene load time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");

			EmitSignal(SignalName.GameInitialized);
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

	private void InitializeMapView() {
		EngineStorage.ReadGameData((GameData gameData) => {
			GameMap map = gameData.map;

			Vector2? cameraLocation = null;
			if (mapView != null) {
				cameraLocation = mapView.cameraLocation;
				RemoveChild(mapView);
			}

			mapView = new MapView(this, map.numTilesWide, map.numTilesTall, map.wrapHorizontally, map.wrapVertically);
			AddChild(mapView);

			mapView.cameraZoom = (float)1.0;
			mapView.gridLayer.visible = false;

			if (!cameraLocation.HasValue) {
				// Set initial camera location. If the UI controller has any cities, focus on their capital. Otherwise, focus on their
				// starting settler.
				if (controller.cities.Count > 0) {
					City capital = controller.cities.Find(c => c.IsCapital());
					if (capital != null)
						mapView.centerCameraOnTile(capital.location);
				} else {
					MapUnit startingSettler =
						controller.units.Find(u => u.unitType.actions.Contains(UnitAction.BuildCity));
					if (startingSettler != null)
						mapView.centerCameraOnTile(startingSettler.location);
				}
			} else {
				mapView.cameraLocation = cameraLocation.Value;
			}

			// Allow the city screen to control whether tile assignments
			// are visible and map UI locations back to map locations.
			cityScreen.tileAssignmentLayer = mapView.tileAssignmentLayer;
			cityScreen.mapView = mapView;
			cityScreen.citizenTypes = gameData.citizenTypes;

			// Allow the domestic advisor to trigger popups.
			advisor.domesticAdvisor.SetPopupOverlay(popupOverlay);
		});
	}

	public void HandleEngineMessage(MessageToUI msg) {
		GameData gameData = EngineStorage.gameData;

		switch (msg) {
			case MsgStartTurn mST:
				OnPlayerStartTurn();
				break;
			case MsgShowCityScreen mSCS:
				ShowCityScreenForCity(gameData, mSCS.city);
				break;
			case MsgCityCreated mCC:
				ShowCityScreenForCity(gameData, mCC.city);
				break;
			case MsgCityDestroyed mCD:
				mapView.cityLayer.UpdateAfterCityDestruction(mCD.city);
				break;
			case MsgCivilizationDestroyed mCivD:
				popupOverlay.ShowPopup(new CivilizationDestroyed(mCivD.civilization), PopupOverlay.PopupCategory.Advisor);

				// Break out of fast forward mode after interesting events.
				turnsLeftToFastForward = 0;
				break;
			case MsgShowMilitaryAdvisorPopup mSMAP:
				if (!popupOverlay.Visible) {
					popupOverlay.ShowPopup(
						new InformationalPopup(mSMAP.message, AdvisorHead.Advisor.Military, mSMAP.happy ? AdvisorHead.Mood.Happy : AdvisorHead.Mood.Angry),
						PopupOverlay.PopupCategory.Advisor);
				}
				break;
			case MsgShowScienceAdvisor mSSA:
				// F6 is the science advisor.
				// TODO: Move the F* key strings to a set of constants/enum.
				EmitSignal(SignalName.ShowSpecificAdvisor, "F6");
				break;
			case MsgUpdateUiAfterDomesticChange mUUASC:
				// F1 is the domestic advisor.
				// TODO: Move the F* key strings to a set of constants/enum.

				// Ensure the citizen moods are correct before displaying
				// them.
				foreach (City c in controller.cities) {
					c.RecalculateCitizenMoods(gameData);
				}
				EmitSignal(SignalName.ShowSpecificAdvisor, "F1");
				break;
			case MsgShowTradeOffer mSTO:
				diplomacy.ShowDealScreenForPlayer(
					mSTO.humanPlayer.id, mSTO.aiPlayer.id,
					humanGives: mSTO.aiWant,
					humanWants: mSTO.aiGive);
				break;
			case MsgDisplayHurryProductionPopup mDHPP:
				if (mDHPP.details.errorMessage != null) {
					popupOverlay.ShowPopup(
						new InformationalPopup(mDHPP.details.errorMessage),
						PopupOverlay.PopupCategory.Advisor);
				} else {
					popupOverlay.ShowPopup(
						new ConfirmationPopup(message: mDHPP.details.costMessage,
												yesText: "Yes I'm sure!",
												noText: "Maybe you're right. Nevermind.",
												yesAction: () => {
													new MsgDoHurryProduction(mDHPP.city).send();
												}),
						PopupOverlay.PopupCategory.Advisor);
				}
				break;
			case MsgWarDeclaration mWD:
				popupOverlay.ShowPopup(
					new InformationalPopup($"The {mWD.aggressor.civilization.noun} declared war on the {mWD.opponent.civilization.noun}"),
					PopupOverlay.PopupCategory.Advisor);

				// Break out of the fast forward mode when something
				// interesting happens.
				turnsLeftToFastForward = 0;
				break;
			case MsgShowTemporaryPopup mSTP:
				TemporaryPopup popup = new(mSTP.message, 1);
				popup.SetPosition(mapView.screenLocationOfTile(mSTP.location, true) + new Vector2(0, -64));
				AddChild(popup);
				popup.ShowPopup();
				break;
			case MsgUnitMoved mUUAAB:
				EmitSignal(SignalName.UnitMoved, new ParameterWrapper<MapUnit>(mUUAAB.Unit));
				break;
		}
	}

	public override void _Process(double delta) {
		ProcessActions();

		if (!EngineStorage.HasPendingAnimations())
			EngineStorage.ProcessNextMessageToEngine();

		if (EngineStorage.TryDequeueNextMessageToUI(out MessageToUI msg))
			HandleEngineMessage(msg);
	}

	// If "location" is not already near the center of the screen, moves the camera to bring it into view.
	public void ensureLocationIsInView(Tile location) {
		if (controller.tileKnowledge.isTileKnown(location) && location != Tile.NONE) {
			Vector2 relativeScreenLocation = mapView.screenLocationOfTile(location, true) / mapView.getVisibleAreaSize();
			if (relativeScreenLocation.DistanceTo(new Vector2((float)0.5, (float)0.5)) > 0.30)
				mapView.centerCameraOnTile(location);
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
		EngineStorage.ReadGameData((GameData gameData) => {
			log.Information("Starting player turn");

			// If the player can now pick a new government, force them to do so.
			// When the popup is closed we call OnPlayerStartTurn again. This isn't
			// ideal, but we don't yet have a general purpose "show a popup and
			// wait for the player to acknowledge it" system.
			if (controller.government.transitionType && TurnHandling.GetTurnNumber() >= controller.inAnarchyUntilTurn) {
				popupOverlay.ShowPopup(
					new GovernmentSelection(controller, controller.GetAvailableGovernments(gameData)),
					PopupOverlay.PopupCategory.Info);
			}

			// If the player can pick a new tech to research, prompt them to do so
			// once they have a city.
			if (controller.cities.Count > 0
					&& controller.currentlyResearchedTech == null
					&& controller.GetAvailableTechsToResearch(gameData.techs).Count > 0) {
				popupOverlay.ShowPopup(
						new ScienceSelection(controller),
						PopupOverlay.PopupCategory.Info);

				if (controller.currentlyResearchedTech == null && controller.GetAvailableTechsToResearch(gameData.techs).Count > 0) {
					PlayerAI.MaybePickTechToResearch(controller, gameData.techs);
				}
			}

			// Allow fast forwarding in observer mode.
			if (gameData.observerMode && turnsLeftToFastForward > 0) {
				--turnsLeftToFastForward;
				new MsgEndTurn().send();
				return;
			}

			CurrentState = GameState.PlayerTurn;
		});

		EmitSignal(SignalName.PlayerTurnStart);
	}

	private void OnPlayerEndTurn() {
		if (CurrentState != GameState.PlayerTurn) {
			return;
		}

		// Prompt the user if they would have a city riot when the turn ended.
		bool doEndTurn = true;
		EngineStorage.ReadGameData((GameData gameData) => {
			foreach (City city in controller.cities) {
				if (!controller.isHuman) {
					continue;
				}

				City.Mood cityMood = city.RecalculateCitizenMoods(gameData);
				if (cityMood == City.Mood.Unhappy) {
					popupOverlay.ShowPopup(
						new ConfirmationPopup(
							$"{city.name} will riot! Are you sure?",
							"Yes, let them riot!",
							"No. Maybe you are right, advisor.",
							() => {
								DoActualEndTurn();
							}),
						PopupOverlay.PopupCategory.Advisor);
					doEndTurn = false;
					return;
				}
			}
		});
		if (doEndTurn) {
			DoActualEndTurn();
		}
	}

	private void DoActualEndTurn() {
		log.Information("Ending player turn");
		EmitSignal(SignalName.TurnEnded);
		log.Information("Starting computer turn");
		CurrentState = GameState.ComputerTurn;
		new MsgEndTurn().send(); // Triggers actual backend processing
		EmitSignal(SignalName.PlayerTurnEnd);
	}

	public void _on_QuitButton_pressed() {
		// This apparently exits the whole program
		// GetTree().Quit();

		// ChangeSceneToFile deletes the current scene and frees its memory, so this is quitting to main menu
		GetTree().ChangeSceneToFile("res://UIElements/MainMenu/main_menu.tscn");
	}

	public void _on_Zoom_value_changed(float value) {
		mapView.setCameraZoomFromMiddle(value);
	}

	public override void _Input(InputEvent @event) {
		if (@event is InputEventKey e && e.Pressed && !e.IsAction(C7Action.UnitGoto)) {
			this.setGotoMode(false);
		}
	}

	public override void _UnhandledInput(InputEvent @event) {
		// Don't handle mouse actions if UI elements are visible
		if (popupOverlay.Visible || cityScreen.Visible || advisor.Visible || diplomacy.Visible || palaceScene.Visible) {
			IsMovingCamera = false;
			return;
		}

		// Control node must not be in the way and/or have mouse pass enabled
		if (@event is InputEventMouseButton eventMouseButton) {
			HandleMouseButtonInput(eventMouseButton);
		} else if (@event is InputEventMouseMotion eventMouseMotion) {
			HandleMouseMotionInput(eventMouseMotion);
		} else if (@event is InputEventKey eventKeyDown && eventKeyDown.Pressed) {
			HandleKeyboardInput(eventKeyDown);
		} else if (@event is InputEventMagnifyGesture magnifyGesture) {
			HandleMagnifyGesture(magnifyGesture);
		}
	}

	private void HandleMouseButtonInput(InputEventMouseButton eventMouseButton) {
		if (eventMouseButton.ButtonIndex == MouseButton.Left) {
			HandleLeftMouseButton(eventMouseButton);
		} else if (eventMouseButton.ButtonIndex == MouseButton.Right && !eventMouseButton.IsPressed()) {
			HandleRightMouseButton(eventMouseButton);
		} else if (eventMouseButton.ButtonIndex == MouseButton.WheelUp) {
			AdjustZoom(0.1f);
		} else if (eventMouseButton.ButtonIndex == MouseButton.WheelDown) {
			AdjustZoom(-0.1f);
		}
	}

	private void AdjustZoom(float delta) {
		float newScale = mapView.cameraZoom + delta;
		mapView.setCameraZoom(newScale, GetViewport().GetMousePosition());
		GetViewport().SetInputAsHandled();
	}

	private void HandleLeftMouseButton(InputEventMouseButton eventMouseButton) {
		GetViewport().SetInputAsHandled();
		if (eventMouseButton.IsPressed()) {
			OldPosition = eventMouseButton.Position;
			IsMovingCamera = true;

			if (CanDoubleClick(eventMouseButton)) {
				doubleClickHandler.Accept(eventMouseButton);
			} else {
				OnSingleLeftMouseButtonClick(eventMouseButton);
			}
		} else {
			IsMovingCamera = false;
		}
	}

	private Tile PositionToTile(Vector2 position) {
		Tile tile = null;
		EngineStorage.ReadGameData((GameData gameData) => {
			tile = mapView.tileOnScreenAt(gameData.map, position);
		});
		return tile;
	}

	private bool CanDoubleClick(InputEventMouseButton eventMouseButton) {
		Tile tile = PositionToTile(eventMouseButton.Position);

		return gotoInfo == null && tile?.cityAtTile?.owner == controller;
	}

	private void OnSingleLeftMouseButtonClick(InputEventMouseButton eventMouseButton) {
		if (gotoInfo != null) {
			HandleGotoClick(gotoInfo);
			setGotoMode(false);
		} else {
			// Select unit on tile at mouse location
			HandleUnitSelection(eventMouseButton);
		}
	}

	private void OnDoubleLeftMouseButtonClick(InputEventMouseButton eventMouseButton) {
		Tile tile = PositionToTile(eventMouseButton.Position);
		if (tile?.cityAtTile?.owner == controller) {
			EngineStorage.ReadGameData((GameData gameData) => {
				ShowCityScreenForCity(gameData, tile.cityAtTile);
			});
		}
	}

	private void HandleUnitSelection(InputEventMouseButton eventMouseButton) {
		Tile tile = PositionToTile(eventMouseButton.Position);
		if (tile == null) {
			return;
		}

		// TODO: This should really be the top unit.
		MapUnit to_select = tile.unitsOnTile.FirstOrDefault();
		if (to_select == null || to_select.owner != controller) {
			return;
		}

		bool canMove = unitSelector.SetSelectedUnit(to_select);
		if (!canMove) {
			TemporaryPopup popup = new("This unit has already moved.", 1);
			popup.SetPosition(eventMouseButton.Position + new Vector2(0, -64));
			AddChild(popup);
			popup.ShowPopup();
		}
	}

	private void HandleRightMouseButton(InputEventMouseButton eventMouseButton) {
		setGotoMode(false);

		Tile tile = PositionToTile(eventMouseButton.Position);
		if (tile != null) {
			HandleRightClickOnTile(tile, eventMouseButton);
		} else {
			log.Debug("Didn't click on any tile");
		}
	}

	private void HandleRightClickOnTile(Tile tile, InputEventMouseButton eventMouseButton) {
		bool shiftDown = Input.IsKeyPressed(Godot.Key.Shift);

		// Handle the shortcut of shift+right clicking a city to get the change production menu.
		if (shiftDown && tile.cityAtTile?.owner == controller)
			new RightClickChooseProductionMenu(this, tile.cityAtTile).Open(eventMouseButton.Position);
		else if (!shiftDown && tile.unitsOnTile.Count > 0)
			// There are units on this title, so open that menu.
			new RightClickTileMenu(this, tile).Open(eventMouseButton.Position);
		else if (!shiftDown && tile.cityAtTile?.owner == controller)
			// There are no units, but this is the player's city.
			new RightClickCityMenu(this, tile).Open(eventMouseButton.Position);

		LogTileDetails(tile);
	}

	private void LogTileDetails(Tile tile) {
		string yield = tile.YieldString(controller);
		log.Debug($"({tile.XCoordinate}, {tile.YCoordinate}): {tile.overlayTerrainType.DisplayName} {yield}");

		if (tile.cityAtTile != null) {
			LogCityDetails(tile.cityAtTile);
		}

		if (tile.unitsOnTile.Count > 0) {
			LogUnitDetails(tile.unitsOnTile);
		}
	}

	private void LogCityDetails(City city) {
		log.Debug($"  {city.name}, production {city.shieldsStored} of {city.owner.ShieldCost(city.itemBeingProduced)}");
		foreach (CityResident resident in city.residents) {
			log.Debug($"  Resident working at {resident.tileWorked}");
		}
	}

	private void LogUnitDetails(List<MapUnit> unitsOnTile) {
		foreach (MapUnit unit in unitsOnTile) {
			log.Debug("  Unit on tile: " + unit);
			if (unit.currentAI != null) {
				log.Debug("  Strategy: " + unit.currentAI.SummarizePlan());
			}
		}
	}

	private void HandleMouseMotionInput(InputEventMouseMotion eventMouseMotion) {
		if (IsMovingCamera) {
			GetViewport().SetInputAsHandled();
			mapView.cameraLocation += OldPosition - eventMouseMotion.Position;
			OldPosition = eventMouseMotion.Position;
		} else if (gotoInfo != null) {
			gotoInfo = GetGotoInfo(eventMouseMotion.Position);
		}
	}

	private void HandleKeyboardInput(InputEventKey eventKeyDown) {
		if (eventKeyDown.Keycode == Godot.Key.O && eventKeyDown.ShiftPressed && eventKeyDown.IsCommandOrControlPressed() && eventKeyDown.AltPressed) {
			ToggleObserverMode();
		}
		if (eventKeyDown.Keycode == Godot.Key.G && eventKeyDown.ShiftPressed && eventKeyDown.IsCommandOrControlPressed() && eventKeyDown.AltPressed) {
			ToggleGridCoordinates();
		}
		if (eventKeyDown.Keycode == Godot.Key.T && eventKeyDown.ShiftPressed && eventKeyDown.IsCommandOrControlPressed() && eventKeyDown.AltPressed) {
			ToggleC7Graphics();
		}
		if (eventKeyDown.Keycode == Godot.Key.F1) {
			EmitSignal(SignalName.ShowSpecificAdvisor, "F1");
		}
		if (eventKeyDown.Keycode == Godot.Key.F3) {
			EmitSignal(SignalName.ShowSpecificAdvisor, "F3");
		}
		if (eventKeyDown.Keycode == Godot.Key.F6) {
			EmitSignal(SignalName.ShowSpecificAdvisor, "F6");
		}
		if (eventKeyDown.Keycode == Godot.Key.F9) {
			palaceScene.Show();
		}
		if (eventKeyDown.Keycode == Godot.Key.C && HasCurrentlySelectedUnit()) {
			mapView.centerCameraOnTile(CurrentlySelectedUnit.location);
		}
		if (eventKeyDown.Keycode == Godot.Key.H) {
			City capital = controller.cities.Find(c => c.IsCapital());
			if (capital != null) {
				mapView.centerCameraOnTile(capital.location);
			}
		}
	}

	private void ToggleObserverMode() {
		EngineStorage.ReadGameData((GameData gameData) => {
			gameData.observerMode = !gameData.observerMode;
			if (gameData.observerMode) {
				SetObserverModeOn(gameData);
			} else {
				SetObserverModeOff(gameData);
			}
		});
	}

	private void SetObserverModeOn(GameData gameData) {
		foreach (Player player in gameData.players) {
			player.isHuman = false;
		}
		animationController.SetAnimationsEnabled(false);
		popupOverlay.ShowPopup(
			new TextDialog("How many turns to fast forward through?",
							"Turns: ", "100",
							BoxContainer.AlignmentMode.Begin,
							(string turns) => { turnsLeftToFastForward = int.Parse(turns); }),
				PopupOverlay.PopupCategory.Advisor);
	}

	private void SetObserverModeOff(GameData gameData) {
		foreach (Player player in gameData.players) {
			if (player.id == EngineStorage.uiControllerID) {
				player.isHuman = true;
			}
		}
	}

	private void ToggleGridCoordinates() {
		EngineStorage.ReadGameData((GameData gameData) => {
			gameData.showGridCoordinates = !gameData.showGridCoordinates;
		});
	}

	private void ToggleC7Graphics() {
		Global.ToggleModernGraphics();
		InitializeMapView();
	}

	private void HandleMagnifyGesture(InputEventMagnifyGesture magnifyGesture) {
		double newScale = mapView.cameraZoom * magnifyGesture.Factor;

		mapView.setCameraZoom((float)newScale, magnifyGesture.Position);
	}

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
			cityScreen.Hide();
			return;
		}

		if (currentAction == C7Action.Escape && palaceScene.Visible) {
			palaceScene.Hide();
			return;
		}

		if (currentAction == C7Action.Escape && advisor.Visible) {
			advisor.Hide();
			return;
		}

		if (currentAction == C7Action.Escape && diplomacy.Visible) {
			diplomacy.Hide();
			return;
		}

		// never poll for actions if UI elements are visible
		if (popupOverlay.Visible || cityScreen.Visible || advisor.Visible || diplomacy.Visible || palaceScene.Visible) {
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
			} else {
				mapView.setCameraZoomFromMiddle(0.5f);
			}
		}

		if (currentAction == C7Action.ToggleAnimations) {
			animationController.SetAnimationsEnabled(false);
		} else if (Input.IsActionJustReleased(C7Action.ToggleAnimations)) {
			animationController.SetAnimationsEnabled(true);
		}

		// actions with unit buttons, which are only relevant during the player
		// turn.
		if (CurrentState != GameState.PlayerTurn) {
			return;
		}

		if (currentAction == C7Action.UnitHold) {
			new ActionToEngineMsg(() => CurrentlySelectedUnit?.skipTurn()).send();
		}

		if (currentAction == C7Action.UnitWait) {
			UnitInteractions.waitUnit(CurrentlySelectedUnit.id);
			unitSelector.SetNextUnit();
		}

		if (currentAction == C7Action.UnitFortify) {
			new MsgSetFortification(CurrentlySelectedUnit.id, true).send();
		}

		if (currentAction == C7Action.UnitDisband) {
			popupOverlay.ShowPopup(
				new ConfirmationPopup(
					$"Disband {CurrentlySelectedUnit.unitType.name}? Pardon me but these are OUR people. Do \nyou really want to disband them?",
					"Yes, we need to!",
					"No. Maybe you are right, advisor.",
					() => {
						new ActionToEngineMsg(() => CurrentlySelectedUnit?.disband()).send();
					}),
				PopupOverlay.PopupCategory.Advisor);
		}

		// unit_goto's behavior is more complicated than other actions - it
		// toggles the go to state, but must be detoggled in _*Input methods if
		// it is not the input being pressed.
		if (currentAction == C7Action.UnitGoto) {
			setGotoMode(true);
		}

		if (currentAction == C7Action.UnitExplore && CurrentlySelectedUnit != MapUnit.NONE) {
			new ActionToEngineMsg(() => CurrentlySelectedUnit?.explore()).send();
		}

		if (currentAction == C7Action.UnitAutomate && CurrentlySelectedUnit != MapUnit.NONE) {
			new ActionToEngineMsg(() => CurrentlySelectedUnit?.automate()).send();
		}

		if (currentAction == C7Action.UnitSentry) {
			// unimplemented
		}

		if (currentAction == C7Action.UnitSentryEnemyOnly) {
			// unimplemented
		}

		if (currentAction == C7Action.UnitBuildCity && CurrentlySelectedUnit != MapUnit.NONE && (CurrentlySelectedUnit?.canBuildCity() ?? false)) {
			EngineStorage.ReadGameData((GameData gameData) => {
				MapUnit currentUnit = gameData.GetUnit(CurrentlySelectedUnit.id);
				log.Debug(currentUnit.Describe());
				if (currentUnit.canBuildCity()) {
					popupOverlay.ShowPopup(new BuildCityDialog(controller.GetNextCityName()),
						PopupOverlay.PopupCategory.Advisor);
				}
			});
		}

		Terraform terraform = C7Action.ToTerraform(currentAction);

		if (CurrentlySelectedUnit == MapUnit.NONE || CurrentlySelectedUnit == null
			|| terraform == null || !CurrentlySelectedUnit.canPerformTerraformAction(terraform))
			return;

		TerrainImprovement replacementTarget = CurrentlySelectedUnit.location.overlays.GetReplacementTarget(terraform);
		if (replacementTarget != null) {
			popupOverlay.ShowPopup(
				new ConfirmationPopup(
					$"A previous terrain enhancement ({replacementTarget.key.Capitalize()}) will be replaced \nby this operation. Do you wish to continue?",
					"Continue.",
					"Cancel action.",
					() => {
						new MsgStartWorkerJob(CurrentlySelectedUnit.id, terraform).send();
					}),
				PopupOverlay.PopupCategory.Advisor);
			return;
		}
		new MsgStartWorkerJob(CurrentlySelectedUnit.id, terraform).send();
	}

	private void setGotoMode(bool isOn) {
		if (isOn) {
			gotoInfo = new();
		} else {
			gotoInfo = null;
		}
	}

	private void HandleGotoClick(GotoInfo info) {
		if (info == null || info.moveCost == -1) {
			return;
		}

		EngineStorage.ReadGameData((GameData gameData) => {
			int currentTurn = gameData.turn;

			// If this move would require declaring war, display a popup that checks
			// if the player really wants to declare war. If they do, declare the
			// war for them, clear out the player, and call this method again.
			if (info.requiresWarDeclarationOnPlayer != null) {
				GotoInfo stashed = info;
				popupOverlay.ShowPopup(new WarConfirmation(stashed.requiresWarDeclarationOnPlayer,
					() => {
						controller.DeclareWarOn(info.requiresWarDeclarationOnPlayer, currentTurn);
						stashed.requiresWarDeclarationOnPlayer = null;
						HandleGotoClick(stashed);
					}), PopupOverlay.PopupCategory.Advisor);
				return;
			}

			new MsgSetUnitPath(CurrentlySelectedUnit.id, info.path).send();
		});
	}

	private GotoInfo GetGotoInfo(Vector2 mousePos) {
		GotoInfo result = new();

		// We're in "goto" mode and moved the mouse over a tile.
		//
		// Figure out which tile it was.
		EngineStorage.ReadGameData((GameData gameData) => {
			Tile tile = mapView.tileOnScreenAt(gameData.map, mousePos);
			result.destinationTile = tile;

			// Figure out what unit is in goto mode. If the tile we're hovering over is
			// different than the tile the unit is on, calculate the path to move there.
			MapUnit unit = tile == null ? null : gameData.GetUnit(CurrentlySelectedUnit.id);
			if (unit != null && unit.location != tile) {
				result.path = PathingAlgorithmChooser.GetAlgorithm(unit).PathFrom(unit.location, tile, unit);
				result.moveCost =
					result.path.PathCost(unit.owner, unit.location, unit.unitType.movement, unit.movementPoints.remaining);
				result.pathCoords = result.path.GetPathCoords();

				// If we couldn't path onto the tile, but the tile is next to us and
				// we could enter the tile if combat is allowed (or if we could
				// declare war with the move) mark the path.
				if (result.moveCost == -1
					&& unit.location.distanceTo(tile) == 1
					&& unit.CanEnterTile(tile, allowCombat: true, allowWarDeclaration: true)) {
					Queue<Tile> pathQueue = new();
					pathQueue.Enqueue(tile);

					result.path = new TilePath(tile, pathQueue);
					result.moveCost = result.path.PathCost(unit.owner, unit.location, unit.unitType.movement,
						unit.movementPoints.remaining);
					result.pathCoords = result.path.GetPathCoords();
					result.attackingMove = true;

					// If we couldn't enter this tile without a war declaration,
					// record which civ we need to declare war on.
					if (!unit.CanEnterTile(tile, allowCombat: true, allowWarDeclaration: false)) {
						if (tile.cityAtTile != null) {
							result.requiresWarDeclarationOnPlayer = tile.cityAtTile.owner;
						} else {
							result.requiresWarDeclarationOnPlayer = tile.unitsOnTile[0].owner;
						}
					}
				}
			} else {
				// Hide the goto cursor, we don't have a valid move.
				result.moveCost = -1;
			}
		});

		return result;
	}

	/**
	 * User quit.  We *may* want to do some things here like make a back-up save, or call the server and let it know we're bailing (esp. in MP).
	 **/
	private void OnQuitTheGame() {
		log.Information("Goodbye!");
		GetTree().Quit();
	}

	private void OnBuildCity(string name) {
		if (CurrentlySelectedUnit != null)
			new MsgBuildCity(CurrentlySelectedUnit, name).send();
	}

	public void ShowCityScreenForCity(GameData gameData, City city) {
		city.RecalculateCitizenMoods(gameData);
		EmitSignal(SignalName.ShowCityScreen, new ParameterWrapper<City>(city));
	}

	public void OnDiplomacySelected(ParameterWrapper<ID> opponentPlayer) {
		diplomacy.ShowTalkScreenForPlayer(controller.id, opponentPlayer.Value);
	}
}
