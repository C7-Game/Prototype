using Godot;
using ConvertCiv3Media;
using C7GameData;
using Serilog;

public partial class LowerRightInfoBox : TextureRect {
	private ILogger log = LogManager.ForContext<LowerRightInfoBox>();

	TextureButton nextTurnButton = new TextureButton();
	ImageTexture nextTurnOnTexture;
	ImageTexture nextTurnOffTexture;
	ImageTexture nextTurnBlinkTexture;

	Label lblUnitSelected = new Label();
	Label attackDefenseMovement = new Label();
	Label terrainType = new Label();
	Label yearAndGold = new Label();
	Label scienceProgress = new();

	Timer blinkingTimer = new Timer();
	bool timerStarted = false;  //This "isStopped" returns false if it's never been started.  So we need this to know if we've ever started it.

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		Pcx boxRightColor = new Pcx(Util.Civ3MediaPath("Art/interface/box right color.pcx"));
		Pcx boxRightAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/box right alpha.pcx"));
		ImageTexture boxRight = PCXToGodot.getImageFromPCXWithAlphaBlend(boxRightColor, boxRightAlpha);
		TextureRect boxRightRectangle = new TextureRect();
		boxRightRectangle.Texture = boxRight;
		boxRightRectangle.SetPosition(new Vector2(0, 0));
		AddChild(boxRightRectangle);

		Pcx nextTurnColor = new Pcx(Util.Civ3MediaPath("Art/interface/nextturn states color.pcx"));
		Pcx nextTurnAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/nextturn states alpha.pcx"));
		nextTurnOffTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(nextTurnColor, nextTurnAlpha, 0, 0, 47, 28);
		nextTurnOnTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(nextTurnColor, nextTurnAlpha, 47, 0, 47, 28);
		nextTurnBlinkTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(nextTurnColor, nextTurnAlpha, 94, 0, 47, 28);

		nextTurnButton.TextureNormal = nextTurnOffTexture;
		nextTurnButton.TextureHover = nextTurnOnTexture;
		nextTurnButton.SetPosition(new Vector2(0, 0));
		AddChild(nextTurnButton);
		nextTurnButton.Pressed += turnEnded;


		//Labels and whatnot in this text box
		lblUnitSelected.Text = "Settler";
		lblUnitSelected.HorizontalAlignment = HorizontalAlignment.Right;
		lblUnitSelected.SetPosition(new Vector2(0, 20));
		lblUnitSelected.AnchorRight = 1.0f;
		lblUnitSelected.OffsetRight = -30;
		boxRightRectangle.AddChild(lblUnitSelected);

		attackDefenseMovement.Text = "0.0. 1/1";
		attackDefenseMovement.HorizontalAlignment = HorizontalAlignment.Right;
		attackDefenseMovement.SetPosition(new Vector2(0, 35));
		attackDefenseMovement.AnchorRight = 1.0f;
		attackDefenseMovement.OffsetRight = -30;
		boxRightRectangle.AddChild(attackDefenseMovement);

		terrainType.Text = "Grassland";
		terrainType.HorizontalAlignment = HorizontalAlignment.Right;
		terrainType.SetPosition(new Vector2(0, 50));
		terrainType.AnchorRight = 1.0f;
		terrainType.OffsetRight = -30;
		boxRightRectangle.AddChild(terrainType);

		Label civAndGovt = new Label();
		civAndGovt.SetPosition(new Vector2(0, 75));
		boxRightRectangle.AddChild(civAndGovt);
		SetTextAndCenterLabel(civAndGovt, "Carthage - Despotism (5.5.0)");

		yearAndGold.SetPosition(new Vector2(0, 90));
		boxRightRectangle.AddChild(yearAndGold);
		SetTextAndCenterLabel(yearAndGold, "Turn 0  10 Gold (+0 per turn)");

		scienceProgress.SetPosition(new Vector2(0, 105));
		boxRightRectangle.AddChild(scienceProgress);
		SetTextAndCenterLabel(scienceProgress, "");

		//Setup up, but do not start, the timer.
		blinkingTimer.OneShot = false;
		blinkingTimer.WaitTime = 0.6f;
		blinkingTimer.Timeout += toggleEndTurnButton;
		AddChild(blinkingTimer);
	}

	public void SetEndOfTurnStatus() {
		lblUnitSelected.Text = "ENTER or SPACEBAR for next turn";
		attackDefenseMovement.Visible = false;
		terrainType.Visible = false;

		toggleEndTurnButton();

		if (!timerStarted) {
			blinkingTimer.Start();
			log.Debug("Started a timer for blinking");

			timerStarted = true;
		}
	}

	private void toggleEndTurnButton() {
		if (nextTurnButton.TextureNormal == nextTurnOnTexture) {
			nextTurnButton.TextureNormal = nextTurnBlinkTexture;
			lblUnitSelected.Visible = true;
		} else {
			nextTurnButton.TextureNormal = nextTurnOnTexture;
			lblUnitSelected.Visible = false;
		}
	}

	public void StopToggling() {
		nextTurnButton.TextureNormal = nextTurnOffTexture;
		lblUnitSelected.Text = "Please wait...";
		lblUnitSelected.Visible = true;
		blinkingTimer.Stop();
		timerStarted = false;
	}

	private void turnEnded() {
		log.Debug("Emitting the blinky button pressed signal");
		GetParent().EmitSignal(GameStatus.SignalName.BlinkyEndTurnButtonPressed);
	}

	public void UpdateUnitInfo(MapUnit NewUnit, TerrainType terrain) {
		terrainType.Text = terrain.DisplayName;
		terrainType.Visible = true;
		lblUnitSelected.Text = NewUnit.unitType.name;
		lblUnitSelected.Visible = true;
		string movementPointsRemaining = NewUnit.movementPoints.canMove ? "" + $"{(NewUnit.movementPoints.getMixedNumber())}" : "0";
		string bombardText = "";
		if (NewUnit.unitType.bombard > 0) {
			bombardText = $"({NewUnit.unitType.bombard})";
		}
		attackDefenseMovement.Text = $"{NewUnit.unitType.attack}{bombardText}.{NewUnit.unitType.defense} {movementPointsRemaining}/{NewUnit.unitType.movement}";
		attackDefenseMovement.Visible = true;
	}

	///This is going to evolve a lot over time.  Probably this info box will need to keep some local state.
	///But for now it'll show the changing turn number, providing some interactivity
	public void SetTurnAndGold(int turnNumber, int gold, int goldPerTurn) {
		if (goldPerTurn >= 0) {
			yearAndGold.Text = $"Turn {turnNumber}  {gold} Gold (+{goldPerTurn} per turn)";
		} else {
			yearAndGold.Text = $"Turn {turnNumber}  {gold} Gold (-{goldPerTurn} per turn)";
		}
	}

	public void UpdateTechProgress(string techName, int turnsRemaining) {
		if (turnsRemaining >= int.MaxValue) {
			SetTextAndCenterLabel(scienceProgress, $"{techName} (-- turns)");
		} else {
			SetTextAndCenterLabel(scienceProgress, $"{techName} ({turnsRemaining} turns)");
		}
	}

	private void SetTextAndCenterLabel(Label label, string text) {
		//For the centered labels, we anchor them center, with equal weight on each side.
		//Then, when they are visible, we add a left margin that's negative and equal to half
		//their width.
		//Seems like there probably is an easier way, but I haven't found it yet.
		label.Text = text;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AnchorLeft = 0.5f;
		label.AnchorRight = 0.5f;
		label.OffsetLeft = -1 * (label.Size.X / 2.0f);

	}
}
