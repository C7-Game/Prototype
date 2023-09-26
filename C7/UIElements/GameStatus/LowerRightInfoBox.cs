using Godot;
using ConvertCiv3Media;
using C7GameData;
using Serilog;

public partial class LowerRightInfoBox : TextureRect
{
	private ILogger log = LogManager.ForContext<LowerRightInfoBox>();

	TextureButton nextTurnButton = new TextureButton();
	ImageTexture nextTurnOnTexture;
	ImageTexture nextTurnOffTexture;
	ImageTexture nextTurnBlinkTexture;

	Label lblUnitSelected = new Label();
	Label attackDefenseMovement = new Label();
	Label terrainType = new Label();
	Label yearAndGold = new Label();
	Label civAndGovt = new Label(){
		Position = new Vector2(0, 90),
		AnchorLeft = 0.5f,
		AnchorRight = 0.5f,
		HorizontalAlignment = HorizontalAlignment.Center,
	};

	Timer blinkingTimer = new Timer();

	bool timerStarted = false; //This "isStopped" returns false if it's never been started.  So we need this to know if we've ever started it.

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.CreateUI();
	}

	private string displayCiv;
	public string PlayerCivilization {
		get => displayCiv;
		set {
			displayCiv = value;
			civAndGovt.Text = $"{displayCiv} - Despotism (5.5.0)";
			civAndGovt.OffsetLeft = -1 * (civAndGovt.Size.X/2.0f);
		}
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
		nextTurnButton.Connect("pressed",new Callable(this,"turnEnded"));


		//Labels and whatnot in this text box
		lblUnitSelected.Text = "Settler";
		lblUnitSelected.HorizontalAlignment = HorizontalAlignment.Right;
		lblUnitSelected.SetPosition(new Vector2(0, 20));
		lblUnitSelected.AnchorRight = 1.0f;
		lblUnitSelected.OffsetRight = -35;
		boxRightRectangle.AddChild(lblUnitSelected);

		attackDefenseMovement.Text = "0.0. 1/1";
		attackDefenseMovement.HorizontalAlignment = HorizontalAlignment.Right;
		attackDefenseMovement.SetPosition(new Vector2(0, 35));
		attackDefenseMovement.AnchorRight = 1.0f;
		attackDefenseMovement.OffsetRight = -35;
		boxRightRectangle.AddChild(attackDefenseMovement);

		terrainType.Text = "Grassland";
		terrainType.HorizontalAlignment = HorizontalAlignment.Right;
		terrainType.SetPosition(new Vector2(0, 50));
		terrainType.AnchorRight = 1.0f;
		terrainType.OffsetRight = -35;
		boxRightRectangle.AddChild(terrainType);

		//For the centered labels, we anchor them center, with equal weight on each side.
		//Then, when they are visible, we add a left margin that's negative and equal to half
		//their width.
		//Seems like there probably is an easier way, but I haven't found it yet.
		boxRightRectangle.AddChild(civAndGovt);
		PlayerCivilization = ""; // set empty string as placeholder

		yearAndGold.Text = "Turn 0  10 Gold (+0 per turn)";
		yearAndGold.HorizontalAlignment = HorizontalAlignment.Center;
		yearAndGold.SetPosition(new Vector2(0, 105));
		yearAndGold.AnchorLeft = 0.5f;
		yearAndGold.AnchorRight = 0.5f;
		boxRightRectangle.AddChild(yearAndGold);
		yearAndGold.OffsetLeft = -1 * (yearAndGold.Size.X/2.0f);

		//Setup up, but do not start, the timer.
		blinkingTimer.OneShot = false;
		blinkingTimer.WaitTime = 0.6f;
		blinkingTimer.Connect("timeout",new Callable(this,"toggleEndTurnButton"));
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

	private void toggleEndTurnButton()
	{
		if (nextTurnButton.TextureNormal == nextTurnOnTexture) {
			nextTurnButton.TextureNormal = nextTurnBlinkTexture;
			lblUnitSelected.Visible = true;
		}
		else {
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
		GetParent().EmitSignal("BlinkyEndTurnButtonPressed");
	}

	public void UpdateUnitInfo(MapUnit NewUnit, TerrainType terrain)
	{
		terrainType.Text = terrain.DisplayName;
		terrainType.Visible = true;
		lblUnitSelected.Text = NewUnit.unitType.name;
		lblUnitSelected.Visible = true;
		string movementPointsRemaining = NewUnit.movementPoints.canMove ? "" + NewUnit.movementPoints.remaining : "0";
		string bombardText = "";
		if (NewUnit.unitType.bombard > 0)
		{
			bombardText = $"({NewUnit.unitType.bombard})";
		}
		attackDefenseMovement.Text = $"{NewUnit.unitType.attack}{bombardText}.{NewUnit.unitType.defense} {movementPointsRemaining}/{NewUnit.unitType.movement}";
		attackDefenseMovement.Visible = true;
	}

	///This is going to evolve a lot over time.  Probably this info box will need to keep some local state.
	///But for now it'll show the changing turn number, providing some interactivity
	public void SetTurn(int turnNumber)
	{
		yearAndGold.Text = $"Turn {turnNumber}  10 Gold (+0 per turn)";
	}
}
