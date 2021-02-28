using Godot;
using System;
using System.Collections;
using ReadCivData.ConvertCiv3Media;

public class Game : Node2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	public string Civ3Path = "D:/Civilization III";
	
	int[,] Map;
	Hashtable Terrmask = new Hashtable();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Now in game!");
		
		this.TerrainAsTileMap();
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

	public void TerrainAsTileMap() {
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		TileMap TM = new TileMap();
		TM.CellSize = new Vector2(64,32);
		// TM.CenteredTextures = true;
		TileSet TS = new TileSet();
		TM.TileSet = TS;

		Pcx PcxTxtr = new Pcx(Civ3Path + "/Art/Terrain/xpgc.pcx");
		Image ImgTxtr = ByteArrayToImage(PcxTxtr.Image, PcxTxtr.Palette, PcxTxtr.Width, PcxTxtr.Height);
		ImageTexture Txtr = new ImageTexture();
		Txtr.CreateFromImage(ImgTxtr, 0);

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
	
	public static Image ByteArrayToImage(byte[] ba, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		Image OutImage = new Image();
		OutImage.Create(width, height, false, Image.Format.Rgba8);
		OutImage.Lock();
		for (int i = 0; i < width * height; i++)
		{
			if (shadows && ba[i] > 239) {
				// using black and transparency
				OutImage.SetPixel(i % width, i / width, Color.Color8(0,0,0, (byte)((255 -ba[i]) * 16)));
				// using the palette color but adding transparency
				// OutImage.SetPixel(i % width, i / width, Color.Color8(palette[ba[i],0], palette[ba[i],1], palette[ba[i],2], (byte)((255 -ba[i]) * 16)));
			} else {
				OutImage.SetPixel(i % width, i / width, Color.Color8(palette[ba[i],0], palette[ba[i],1], palette[ba[i],2], ba[i] == 255 ? (byte)0 : (byte)255));
			}
		}
		OutImage.Unlock();

		return OutImage;
	}
}
