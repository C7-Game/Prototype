using Godot;
using System;
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

	Timer blinkingTimer = new Timer();
	Boolean timerStarted = false;	//This "isStopped" returns false if it's never been started.  So we need this to know if we've ever started it.

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.CreateUI();
	}

	private void CreateUI() {
		Pcx boxRightColor = new Pcx(Util.Civ3MediaPath("Art/interface/box right color.pcx"));
		Pcx boxRightAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/box right alpha.pcx"));
		ImageTexture boxRight = PCXToGodot.getImageFromPCXWithAlphaBlend(boxRightColor, boxRightAlpha);
		TextureRect boxRightRectangle = new TextureRect();
		boxRightRectangle.Texture2D = boxRight;
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
		lblUnitSelected.Align = Label.AlignEnum.Right;
		lblUnitSelected.SetPosition(new Vector2(0, 20));
		lblUnitSelected.AnchorRight = 1.0f;
		lblUnitSelected.OffsetRight = -35;
		boxRightRectangle.AddChild(lblUnitSelected);

		attackDefenseMovement.Text = "0.0. 1/1";
		attackDefenseMovement.Align = Label.AlignEnum.Right;
		attackDefenseMovement.SetPosition(new Vector2(0, 35));
		attackDefenseMovement.AnchorRight = 1.0f;
		attackDefenseMovement.OffsetRight = -35;
		boxRightRectangle.AddChild(attackDefenseMovement);

		terrainType.Text = "Grassland";
		terrainType.Align = Label.AlignEnum.Right;
		terrainType.SetPosition(new Vector2(0, 50));
		terrainType.AnchorRight = 1.0f;
		terrainType.OffsetRight = -35;
		boxRightRectangle.AddChild(terrainType);

		//For the centered labels, we anchor them center, with equal weight on each side.
		//Then, when they are visible, we add a left margin that's negative and equal to half
		//their width.
		//Seems like there probably is an easier way, but I haven't found it yet.
		Label civAndGovt = new Label();
		civAndGovt.Text = "Carthage - Despotism (5.5.0)";
		civAndGovt.Align = Label.AlignEnum.Center;
		civAndGovt.SetPosition(new Vector2(0, 90));
		civAndGovt.AnchorLeft = 0.5f;
		civAndGovt.AnchorRight = 0.5f;
		boxRightRectangle.AddChild(civAndGovt);
		civAndGovt.OffsetLeft = -1 * (civAndGovt.RectSize.x/2.0f);

		yearAndGold.Text = "Turn 0  10 Gold (+0 per turn)";
		yearAndGold.Align = Label.AlignEnum.Center;
		yearAndGold.SetPosition(new Vector2(0, 105));
		yearAndGold.AnchorLeft = 0.5f;
		yearAndGold.AnchorRight = 0.5f;
		boxRightRectangle.AddChild(yearAndGold);
		yearAndGold.OffsetLeft = -1 * (yearAndGold.RectSize.x/2.0f);

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
		yearAndGold.Text = "Turn " + turnNumber + "  10 Gold (+0 per turn)";
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//
//  }
}
