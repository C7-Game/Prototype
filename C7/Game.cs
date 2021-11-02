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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
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

		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtons.pcx"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtonsAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 0, 1, 35, 28);
		TextureButton menuButton = new TextureButton();
		menuButton.TextureNormal = menuTexture;
		menuButton.SetPosition(new Vector2(21, 12));
		AddChild(menuButton);
		
		ImageTexture civilopediaTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 36, 1, 35, 28);
		TextureButton civilopediaButton = new TextureButton();
		civilopediaButton.TextureNormal = civilopediaTexture;
		civilopediaButton.SetPosition(new Vector2(57, 12));
		AddChild(civilopediaButton);
		
		ImageTexture advisorsTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 73, 1, 35, 28);
		TextureButton advisorsButton = new TextureButton();
		advisorsButton.TextureNormal = advisorsTexture;
		advisorsButton.SetPosition(new Vector2(94, 12));
		AddChild(advisorsButton);

		Pcx boxRightColor = new Pcx(Util.Civ3MediaPath("Art/interface/box right color.pcx"));
		Pcx boxRightAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/box right alpha.pcx"));
		ImageTexture boxRight = PCXToGodot.getImageFromPCXWithAlphaBlend(boxRightColor, boxRightAlpha);
		TextureRect boxRightRectangle = new TextureRect();
		boxRightRectangle.Texture = boxRight;
		boxRightRectangle.SetPosition(new Vector2(OS.WindowSize.x - (boxRightColor.Width + 5), OS.WindowSize.y - (boxRightColor.Height + 1)));
		AddChild(boxRightRectangle);
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
	}

	private void OnPlayerEndTurn()
	{
		if (CurrentState == GameState.PlayerTurn)
		{
			GD.Print("Ending player turn");
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
}
