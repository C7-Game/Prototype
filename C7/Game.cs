using Godot;
using System;
using System.Collections;
using ConvertCiv3Media;

public class Game : Node2D
{
	[Signal] public delegate void TurnStarted();
	enum GameState {
		PreGame,
		PlayerTurn,
		ComputerTurn
	}
	
	int[,] Map;
	Hashtable Terrmask = new Hashtable();
	GameState CurrentState = GameState.PreGame;
	Button EndTurnButton;
	Timer endTurnBlinkingTimer;
    private bool MoveCamera;
    private Vector2 OldPosition;
    private KinematicBody2D Player;

	
	TextureButton nextTurnButton = new TextureButton();
	ImageTexture nextTurnOnTexture;
	ImageTexture nextTurnOffTexture;
	ImageTexture nextTurnBlinkTexture;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Player = GetNode<KinematicBody2D>("KinematicBody2D");
		this.TerrainAsTileMap();
		this.CreateUI();
		ComponentManager.Instance.AddComponent(new TurnCounterComponent());
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
	}

	private void StartGame()
	{
		GD.Print("Game starting");
		TurnCounterComponent turnCntCpnt = ComponentManager.Instance.GetComponent<TurnCounterComponent>();
		Connect(nameof(TurnStarted), turnCntCpnt, nameof(turnCntCpnt.OnTurnStarted));
		OnPlayerStartTurn();
	}

	public void TerrainAsTileMap() {
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		TileMap TM = new TileMap();
		TM.CellSize = new Vector2(64,32);
		// TM.CenteredTextures = true;
		TileSet TS = new TileSet();
		TM.TileSet = TS;

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

		int mywidth = 14, myheight = 18;
		Map = new int[mywidth,myheight];
		// Populate map values, 0 out terrain mask
		for (int y = 0; y < myheight; y++) {
			for (int x = 0; x < mywidth; x++) {
				// If x & y are both even or odd, terrain value; if mismatched, terrain mask init to 0
				Map[x,y] = x%2 - y%2 == 0 ? (new Random()).Next(0,3) : 0;
			}
		}
		// Loop to lookup tile ids based on terrain mask
		for (int y = 0; y < myheight; y++) {
			for (int x = (1 - (y % 2)); x < mywidth; x+=2) {
				int Top = y == 0 ? (Map[(x+1) % mywidth,y]) : (Map[x,y-1]);
				int Bottom = y == myheight - 1 ? (Map[(x+1) % mywidth,y]) : (Map[x,y+1]);
				string foo = 
					(Map[(x+1) % mywidth,y]).ToString("D3") +
					Bottom.ToString("D3") +
					(Map[Mathf.Abs((x-1) % mywidth),y]).ToString("D3") +
					Top.ToString("D3")
				;
				try {
				// Map[x,y] = (int)Terrmask["001001001001"];
				Map[x,y] = (int)Terrmask[foo];
				} catch { GD.Print(x + "," + y + " " + foo); }
			}
		}
		// loop to place tiles, each of which contains 1/4 of 4 'real' map locations
		for (int y = 0; y < myheight; y++) {
			for (int x = 1 - (y%2); x < mywidth; x+=2) {
				// TM.SetCellv(new Vector2(x + (y % 2), y), (new Random()).Next() % TS.GetTilesIds().Count);
				// try {
				TM.SetCellv(new Vector2(x, y), Map[x,y]);
				// } catch {}
			}
		}
		AddChild(TM);
	}

	private void CreateUI()
	{
		EndTurnButton = new Button();
		EndTurnButton.Text = "End Turn";
		EndTurnButton.SetPosition(new Vector2(250, 10));
		AddChild(EndTurnButton);
		EndTurnButton.Connect("pressed", this, "_onEndTurnButtonPressed");

		AddTopLeftButtons();
		AddLowerRightBox();
	}

	private void AddTopLeftButtons()
	{
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtons.pcx"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtonsAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 0, 1, 35, 29);
		TextureButton menuButton = new TextureButton();
		menuButton.TextureNormal = menuTexture;
		menuButton.SetPosition(new Vector2(21, 12));
		AddChild(menuButton);
		
		ImageTexture civilopediaTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 36, 1, 35, 29);
		TextureButton civilopediaButton = new TextureButton();
		civilopediaButton.TextureNormal = civilopediaTexture;
		civilopediaButton.SetPosition(new Vector2(57, 12));
		AddChild(civilopediaButton);
		
		ImageTexture advisorsTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 73, 1, 35, 29);
		TextureButton advisorsButton = new TextureButton();
		advisorsButton.TextureNormal = advisorsTexture;
		advisorsButton.SetPosition(new Vector2(94, 12));
		AddChild(advisorsButton);
	}

	private void AddLowerRightBox()
	{
		Pcx boxRightColor = new Pcx(Util.Civ3MediaPath("Art/interface/box right color.pcx"));
		Pcx boxRightAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/box right alpha.pcx"));
		ImageTexture boxRight = PCXToGodot.getImageFromPCXWithAlphaBlend(boxRightColor, boxRightAlpha);
		TextureRect boxRightRectangle = new TextureRect();
		boxRightRectangle.Texture = boxRight;
		boxRightRectangle.SetPosition(new Vector2(OS.WindowSize.x - (boxRightColor.Width + 5), OS.WindowSize.y - (boxRightColor.Height + 1)));
		AddChild(boxRightRectangle);

		Pcx nextTurnColor = new Pcx(Util.Civ3MediaPath("Art/interface/nextturn states color.pcx"));
		Pcx nextTurnAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/nextturn states alpha.pcx"));
		nextTurnOffTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(nextTurnColor, nextTurnAlpha, 0, 0, 47, 28);
		nextTurnOnTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(nextTurnColor, nextTurnAlpha, 47, 0, 47, 28);
		nextTurnBlinkTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(nextTurnColor, nextTurnAlpha, 94, 0, 47, 28);

		nextTurnButton.TextureNormal = nextTurnOffTexture;
		nextTurnButton.TextureHover = nextTurnOnTexture;
		nextTurnButton.SetPosition(new Vector2(OS.WindowSize.x - (boxRightColor.Width + 5), OS.WindowSize.y - (boxRightColor.Height + 1)));
		AddChild(nextTurnButton);
		nextTurnButton.Connect("pressed", this, "_onEndTurnButtonPressed");


		//Labels and whatnot in this text box
		Label lblUnitSelected = new Label();
		lblUnitSelected.Text = "Settler";
		lblUnitSelected.AddColorOverride("font_color", new Color(0, 0, 0));
		lblUnitSelected.Align = Label.AlignEnum.Right;
		lblUnitSelected.SetPosition(new Vector2(0, 20));
		lblUnitSelected.AnchorRight = 1.0f;
		lblUnitSelected.MarginRight = -35;
		boxRightRectangle.AddChild(lblUnitSelected);
		
		Label attackDefenseMovement = new Label();
		attackDefenseMovement.Text = "0.0. 1/1";
		attackDefenseMovement.AddColorOverride("font_color", new Color(0, 0, 0));
		attackDefenseMovement.Align = Label.AlignEnum.Right;
		attackDefenseMovement.SetPosition(new Vector2(0, 35));
		attackDefenseMovement.AnchorRight = 1.0f;
		attackDefenseMovement.MarginRight = -35;
		boxRightRectangle.AddChild(attackDefenseMovement);
		
		Label terrainType = new Label();
		terrainType.Text = "Grassland";
		terrainType.AddColorOverride("font_color", new Color(0, 0, 0));
		terrainType.Align = Label.AlignEnum.Right;
		terrainType.SetPosition(new Vector2(0, 50));
		terrainType.AnchorRight = 1.0f;
		terrainType.MarginRight = -35;
		boxRightRectangle.AddChild(terrainType);
		
		//For the centered labels, we anchor them center, with equal weight on each side.
		//Then, when they are visible, we add a left margin that's negative and equal to half
		//their width.
		//Seems like there probably is an easier way, but I haven't found it yet.
		Label civAndGovt = new Label();
		civAndGovt.Text = "Rome - Despotism (5.5.0)";
		civAndGovt.AddColorOverride("font_color", new Color(0, 0, 0));
		civAndGovt.Align = Label.AlignEnum.Center;
		civAndGovt.SetPosition(new Vector2(0, 90));
		civAndGovt.AnchorLeft = 0.5f;
		civAndGovt.AnchorRight = 0.5f;
		boxRightRectangle.AddChild(civAndGovt);
		civAndGovt.MarginLeft = -1 * (civAndGovt.RectSize.x/2.0f);

		Label yearAndGold = new Label();
		yearAndGold.Text = "4000 BC  10 Gold (+0 per turn)";
		yearAndGold.AddColorOverride("font_color", new Color(0, 0, 0));
		yearAndGold.Align = Label.AlignEnum.Center;
		yearAndGold.SetPosition(new Vector2(0, 105));
		yearAndGold.AnchorLeft = 0.5f;
		yearAndGold.AnchorRight = 0.5f;
		boxRightRectangle.AddChild(yearAndGold);
		yearAndGold.MarginLeft = -1 * (yearAndGold.RectSize.x/2.0f);
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

		//Set a timer so the end turn button starts blinking after awhile.
		//Obviously once we have more game mechanics, it won't happen automatically
		//after 5 seconds.
		endTurnBlinkingTimer = new Timer();
		endTurnBlinkingTimer.WaitTime = 5.0f;
		endTurnBlinkingTimer.OneShot = true;
		endTurnBlinkingTimer.Connect("timeout", this, "toggleEndTurnButton");
		AddChild(endTurnBlinkingTimer);
		endTurnBlinkingTimer.Start();
	}

	private void toggleEndTurnButton() {
		if (nextTurnButton.TextureNormal == nextTurnOnTexture) {
			nextTurnButton.TextureNormal = nextTurnBlinkTexture;
		}
		else {
			nextTurnButton.TextureNormal = nextTurnOnTexture;
		}
		endTurnBlinkingTimer.OneShot = false;
		endTurnBlinkingTimer.WaitTime = 0.6f;
		endTurnBlinkingTimer.Start();
	}

	private void OnPlayerEndTurn()
	{
		if (CurrentState == GameState.PlayerTurn)
		{
			GD.Print("Ending player turn");
			endTurnBlinkingTimer.Stop();
			nextTurnButton.TextureNormal = nextTurnOffTexture;
			EndTurnButton.Disabled = true;
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
        // NOTE: I think this quits the current node or scene and not necessarily the whole program if this is a child node?
        GetTree().Quit();
    }

    public void _on_Zoom_value_changed(float value)
    {
        Vector2 NewScale = new Vector2(value, value);
        Scale = NewScale;
    }
    public void _on_RightButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x + 128, Player.Position.y);
    }
    public void _on_LeftButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x - 128, Player.Position.y);
    }
    public void _on_UpButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x, Player.Position.y - 64);
    }
    public void _on_DownButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x, Player.Position.y + 64);
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
                    MoveCamera = true;
                }
                else
                {
                    MoveCamera = false;
                }
            }
            else if(eventMouseButton.ButtonIndex == (int)ButtonList.WheelUp)
            {
                GetTree().SetInputAsHandled();
                GetNode<HSlider>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/Zoom").Value += (float)0.1;
            }
            else if(eventMouseButton.ButtonIndex == (int)ButtonList.WheelDown)
            {
                GetTree().SetInputAsHandled();
                GetNode<HSlider>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/Zoom").Value -= (float)0.1;
            }
        }
        else if(@event is InputEventMouseMotion eventMouseMotion)
        {
            if(MoveCamera)
            {
                GetTree().SetInputAsHandled();
                Player.Position += (OldPosition - eventMouseMotion.Position) / Scale;
                OldPosition = eventMouseMotion.Position;
            }
        }
    }

}
