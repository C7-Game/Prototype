using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using ConvertCiv3Media;

public class OldLegacyMap : Node2D
{
    public interface ILegacyTile
    // Tiles need to provide this info to LegacyMap
    {
        // temp hack for prototyping land/water only map
        bool IsLand {get;}
        int LegacyBaseTerrainID { get; }
        int LegacyOverlayTerrainID { get; }
        int LegacyX {get;}
        int LegacyY {get;}
        
    }
    public IEnumerable<ILegacyTile> LegacyTiles;
    private DynamicFont MapFont;
	int[,] Map;
	Hashtable Terrmask;
    TileMap TM;
    public override void _Ready()
    {
        string FontPath = Util.GetCiv3Path() + @"/LSANS.TTF";
        MapFont = new DynamicFont();
        MapFont.FontData = ResourceLoader.Load(FontPath) as DynamicFontData;
    }
	public void TerrainAsTileMap() {
        if (TM != null) { RemoveChild(TM); }
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		TM = new TileMap();
		TM.CellSize = new Vector2(64,32);
		// TM.CenteredTextures = true;
		TileSet TS = new TileSet();
		TM.TileSet = TS;

		Pcx PcxTxtr = new Pcx(Util.Civ3MediaPath("Art/Terrain/xpgc.pcx"));
		ImageTexture Txtr = PCXToGodot.getImageTextureFromPCX(PcxTxtr);
        // Quick hack to map graphic coordinate system to default BIQ terrain ID
        Terrmask = new Hashtable();
        Hashtable TerrID = new Hashtable
        {
            { 0, 1 },
            { 1, 2 },
            { 2, 11 },
        };

		int id = TS.GetLastUnusedTileId();
        // Make blank default tile
        // TODO: Make red tile or similar
        TS.CreateTile(id);
        id++;
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

        // TODO: Don't hard-code size
		int mywidth = 100, myheight = 100;
		Map = new int[mywidth,myheight];

		// Populate map values
        if(LegacyTiles != null)
        {
            foreach (ILegacyTile tile in LegacyTiles)
            {
                Map[tile.LegacyX,tile.LegacyY] = tile.LegacyBaseTerrainID;
            }
        }
		// Loop to lookup tile ids based on terrain mask
		for (int y = 0; y < myheight; y++) {
			for (int x = (1 - (y % 2)); x < mywidth; x+=2) {
				int Top = y == 0 ? (Map[(x+1) % mywidth,y]) : (Map[x,y-1]);
				int Bottom = y == myheight - 1 ? (Map[(x+1) % mywidth,y]) : (Map[x,y+1]);
				int foo = 
					((Map[(x+1) % mywidth,y]) << 12) +
					(Bottom << 8) +
					((Map[Mathf.Abs((x-1) % mywidth),y]) << 4) +
					Top
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
        TM.Scale = new Vector2((float)0.2, (float)0.2);
		AddChild(TM);
	}
}
