using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using ConvertCiv3Media;

public class LegacyMap : Node2D
{
    public interface ILegacyTile
    // Tiles need to provide this info to LegacyMap
    {
        int LegacyFileID { get; }
        int LegacyImageID { get; }
        int LegacyX {get;}
        int LegacyY {get;}
	}
    public IEnumerable<ILegacyTile> LegacyTiles;
	int[,] Map;
    TileMap TM;
	TileSet TS;
	private int[,] TileIDLookup;
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

        // TODO: Don't hard-code size
		int mywidth = 100, myheight = 100;
		Map = new int[mywidth,myheight];

		// Populate map values
        if(LegacyTiles != null)
        {
            foreach (ILegacyTile tile in LegacyTiles)
            {
				// If tile media file not loaded yet
				if(TileIDLookup[tile.LegacyFileID,1] == 0) { LoadTileSet(tile.LegacyFileID); }
                Map[tile.LegacyX,tile.LegacyY] = TileIDLookup[tile.LegacyFileID,tile.LegacyImageID];
            }
        }
		for (int y = 0; y < myheight; y++) {
			for (int x = y % 2; x < mywidth; x+=2) {
				TM.SetCellv(new Vector2(x + 1, y), Map[x,y]);
			}
		}
        // TM.Scale = new Vector2((float)0.2, (float)0.2);
		AddChild(TM);
	}
	private void LoadTileSet(int fileID)
	{
		int id = TS.GetLastUnusedTileId();
		// temp if
		if(fileID == 1)
		{
		Pcx PcxTxtr = new Pcx(Util.Civ3MediaPath("Art/Terrain/xpgc.pcx"));
		ImageTexture Txtr = PCXToGodot.getImageTextureFromPCX(PcxTxtr);

		for (int y = 0; y < PcxTxtr.Height; y += 64) {
			for (int x = 0; x < PcxTxtr.Width; x+= 128, id++) {
				TS.CreateTile(id);
				TS.TileSetTexture(id, Txtr);
				TS.TileSetRegion(id, new Rect2(x, y, 128, 64));
				GD.Print((x / 128) * (PcxTxtr.Height / 64) + (y / 64));
				TileIDLookup[fileID, (x / 128) * (PcxTxtr.Height / 64) + (y / 64)] = id;
			}
		}
		}
	}
}
