using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using ConvertCiv3Media;

public class Civ3Map : Node2D
{
	public class Civ3Tile : C7GameData.Tile, ICiv3Tile
	{
		// stubbing out interface getters
		public int Civ3FileID => 0;
		public int Civ3ImageID => 0;
		public int Civ3X => 0;
		public int Civ3Y => 0;
	}
	public interface ICiv3Tile
	// Tiles need to provide this info to Civ3Map
	{
		int Civ3FileID { get; }
		int Civ3ImageID { get; }
		int Civ3X {get;}
		int Civ3Y {get;}
	}
	public IEnumerable<ICiv3Tile> Civ3Tiles;
	int[,] Map;
	TileMap TM;
	TileSet TS;
	private int[,] TileIDLookup;
	// NOTE: The following two must be set externally before displaying map
	public int MapWidth;
	public int MapHeight;
	// If a mod is in effect, set this, otherwise set to "" or "Conquests"
	public string ModRelPath = "";
	public override void _Ready()
	{
		//
	}
	public void TerrainAsTileMap() {
		if (TM != null) { RemoveChild(TM); }
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		TM = new TileMap();
		TM.CellSize = new Vector2(64,32);
		// TM.CenteredTextures = true;
		TS = new TileSet();
		TM.TileSet = TS;

		TileIDLookup = new int[9,81];

		int id = TS.GetLastUnusedTileId();
		// Make blank default tile
		// TODO: Make red tile or similar
		TS.CreateTile(id);
		id++;

		Map = new int[MapWidth,MapHeight];

		// Populate map values
		if(Civ3Tiles != null)
		{
			foreach (ICiv3Tile tile in Civ3Tiles)
			{
				// If tile media file not loaded yet
				if(TileIDLookup[tile.Civ3FileID,1] == 0) { LoadTileSet(tile.Civ3FileID); }
				Map[tile.Civ3X,tile.Civ3Y] = TileIDLookup[tile.Civ3FileID,tile.Civ3ImageID];
			}
		}
		for (int y = 0; y < MapHeight; y++) {
			for (int x = y % 2; x < MapWidth; x+=2) {
				TM.SetCellv(new Vector2(x, y), Map[x,y]);
			}
		}
		// TM.Scale = new Vector2((float)0.2, (float)0.2);
		AddChild(TM);
	}
	private void LoadTileSet(int fileID)
	{
		Hashtable FileNameLookup = new Hashtable
		{
			{ 0, "Art/Terrain/xtgc.pcx" },
			{ 1, "Art/Terrain/xpgc.pcx" },
			{ 2, "Art/Terrain/xdgc.pcx" },
			{ 3, "Art/Terrain/xdpc.pcx" },
			{ 4, "Art/Terrain/xdgp.pcx" },
			{ 5, "Art/Terrain/xggc.pcx" },
			{ 6, "Art/Terrain/wCSO.pcx" },
			{ 7, "Art/Terrain/wSSS.pcx" },
			{ 8, "Art/Terrain/wOOO.pcx" },
		};
		int id = TS.GetLastUnusedTileId();
		// temp if
		if(FileNameLookup[fileID] != null)
		{
		Pcx PcxTxtr = new Pcx(Util.Civ3MediaPath(FileNameLookup[fileID].ToString(), ModRelPath));
		ImageTexture Txtr = PCXToGodot.getImageTextureFromPCX(PcxTxtr);

		for (int y = 0; y < PcxTxtr.Height; y += 64) {
			for (int x = 0; x < PcxTxtr.Width; x+= 128, id++) {
				TS.CreateTile(id);
				TS.TileSetTexture(id, Txtr);
				TS.TileSetRegion(id, new Rect2(x, y, 128, 64));
				TileIDLookup[fileID, (x / 128) + (y / 64) * (PcxTxtr.Width / 128)] = id;
			}
		}
		}
	}
}
