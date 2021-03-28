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
		TileSet TS = new TileSet();
		TM.TileSet = TS;

		/* Will adapt this to file/index soon
		Pcx PcxTxtr = new Pcx(Util.Civ3MediaPath("Art/Terrain/xpgc.pcx"));
		ImageTexture Txtr = PCXToGodot.getImageTextureFromPCX(PcxTxtr);
		*/
        // Quick hack to map graphic coordinate system to default BIQ terrain ID

		int id = TS.GetLastUnusedTileId();
        // Make blank default tile
        // TODO: Make red tile or similar
        TS.CreateTile(id);
        id++;
		/*
		for (int y = 0; y < PcxTxtr.Height; y += 64) {
			for (int x = 0; x < PcxTxtr.Width; x+= 128, id++) {
				TS.CreateTile(id);
				TS.TileSetTexture(id, Txtr);
				TS.TileSetRegion(id, new Rect2(x, y, 128, 64));
				// order right, bottom, left, top; 0 is plains, 1 grass, 2 coast
                // Temp hack: assuming 4-bit terrain IDs, bit-rotating them into one integer key
                // TODO: Make the key an Array of IEquatable of some sort, most likely int or GUID
				Terrmask.Add(
					((int)TerrID[((y / 64) % 3)] << 12) +
					((int)TerrID[((y / 64) / 3 % 3)] << 8) +
					((int)TerrID[((x / 128) / 3 % 3)] << 4) +
					(int)TerrID[((x / 128) % 3)]
					, id);
			}
		}
		*/

        // TODO: Don't hard-code size
		int mywidth = 100, myheight = 100;
		Map = new int[mywidth,myheight];

		// Populate map values
        if(LegacyTiles != null)
        {
            foreach (ILegacyTile tile in LegacyTiles)
            {
                Map[tile.LegacyX,tile.LegacyY] = tile.LegacyImageID;
            }
        }
        // TM.Scale = new Vector2((float)0.2, (float)0.2);
		AddChild(TM);
	}
}
