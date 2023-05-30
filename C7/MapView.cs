using System.Collections.Generic;
using System;
using System.Linq;
using C7.Map;
using Godot;
using ConvertCiv3Media;
using C7GameData;
using C7Engine;
using Serilog;
using Serilog.Events;

// Loose layers are for drawing things on the map on a per-tile basis. (Historical aside: There used to be another kind of layer called a TileLayer
// that was intended to draw regularly tiled objects like terrain sprites but using LooseLayers for everything was found to be a prefereable
// approach.) LooseLayer is effectively the standard map layer. The MapView contains a list of loose layers, inside a LooseView object. Right now to
// add a new layer you must modify the MapView constructor to add it to the list, but (TODO) eventually that will be made moddable.
public abstract class LooseLayer {
	// drawObject draws the things this layer is supposed to draw that are associated with the given tile. Its parameters are:
	//   looseView: The Node2D to actually draw to, e.g., use looseView.DrawCircle(...) to draw a circle. This object also contains a reference to
	//     the MapView in case you need it.
	//   gameData: A reference to the game data so each layer doesn't have to redundantly request access.
	//   tile: The game tile whose contents are to be drawn. This function gets called for each tile in view of the camera and none out of
	//     view. The same tile may be drawn multiple times at different locations due to edge wrapping.
	//   tileCenter: The location to draw to. You should draw around this location without adjusting for the camera location or zoom since the
	//     MapView already transforms the looseView node to account for those things.
	public abstract void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter);

	public virtual void onBeginDraw(LooseView looseView, GameData gameData) {}
	public virtual void onEndDraw(LooseView looseView, GameData gameData) {}

	// The layer will be skipped during map drawing if visible is false
	public bool visible = true;
}

public partial class TerrainLayer : LooseLayer {

	public static readonly Vector2 terrainSpriteSize = new Vector2(128, 64);

	// A triple sheet is a sprite sheet containing sprites for three different terrain types including transitions between.
	private List<ImageTexture> tripleSheets;

	// TileToDraw stores the arguments passed to drawObject so the draws can be sorted by texture before being submitted. This significantly
	// reduces the number of draw calls Godot must generate (1483 to 312 when fully zoomed out on our test map) and modestly improves framerate
	// (by about 14% on my system).
	private class TileToDraw : IComparable<TileToDraw>
	{
		public Tile tile;
		public Vector2 tileCenter;

		public TileToDraw(Tile tile, Vector2 tileCenter)
		{
			this.tile = tile;
			this.tileCenter = tileCenter;
		}

		public int CompareTo(TileToDraw other)
		{
			// "other" might be null, in which case we should return a positive value. CompareTo(null) will do this.
			try {
				return this.tile.ExtraInfo.BaseTerrainFileID.CompareTo(other?.tile.ExtraInfo.BaseTerrainFileID);
			} catch (Exception) {
				//It also could be Tile.NONE.  In which case, also return a positive value.
				return 1;
			}
		}
	}

	private List<TileToDraw> tilesToDraw = new List<TileToDraw>();

	public TerrainLayer()
	{
		tripleSheets = loadTerrainTripleSheets();
	}

	public List<ImageTexture> loadTerrainTripleSheets()
	{
		List<string> fileNames = new List<string> {
			"Art/Terrain/xtgc.pcx",
			"Art/Terrain/xpgc.pcx",
			"Art/Terrain/xdgc.pcx",
			"Art/Terrain/xdpc.pcx",
			"Art/Terrain/xdgp.pcx",
			"Art/Terrain/xggc.pcx",
			"Art/Terrain/wCSO.pcx",
			"Art/Terrain/wSSS.pcx",
			"Art/Terrain/wOOO.pcx",
		};
		return fileNames.ConvertAll(name => Util.LoadTextureFromPCX(name));
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		tilesToDraw.Add(new TileToDraw(tile, tileCenter));
		tilesToDraw.Add(new TileToDraw(tile.neighbors[TileDirection.SOUTH], tileCenter + new Vector2(0, 64)));
		tilesToDraw.Add(new TileToDraw(tile.neighbors[TileDirection.SOUTHWEST], tileCenter + new Vector2(-64, 32)));
		tilesToDraw.Add(new TileToDraw(tile.neighbors[TileDirection.SOUTHEAST], tileCenter + new Vector2(64, 32)));
	}

	public override void onEndDraw(LooseView looseView, GameData gameData) {
		tilesToDraw.Sort();
		foreach (TileToDraw tTD in tilesToDraw) {
			if (tTD.tile != Tile.NONE) {
				int xSheet = tTD.tile.ExtraInfo.BaseTerrainImageID % 9, ySheet = tTD.tile.ExtraInfo.BaseTerrainImageID / 9;
				Rect2 texRect = new Rect2(new Vector2(xSheet, ySheet) * terrainSpriteSize, terrainSpriteSize);
				Vector2 terrainOffset = new Vector2(0, -1 * MapView.cellSize.Y);
				//Multiply size by 100.1% so avoid "seams" in the map.  See issue #106.
				//Jim's option of a whole-map texture is less hacky, but this is quicker and seems to be working well.
				Rect2 screenRect = new Rect2(tTD.tileCenter - (float)0.5 * terrainSpriteSize + terrainOffset, terrainSpriteSize * 1.001f);
				looseView.DrawTextureRectRegion(tripleSheets[tTD.tile.ExtraInfo.BaseTerrainFileID], screenRect, texRect);
			}
		}
		tilesToDraw.Clear();
	}
}

public partial class HillsLayer : LooseLayer {
	public static readonly Vector2 mountainSize = new Vector2(128, 88);
	public static readonly Vector2 volcanoSize = new Vector2(128, 88);	//same as mountain
	public static readonly Vector2 hillsSize = new Vector2(128, 72);
	private ImageTexture mountainTexture;
	private ImageTexture snowMountainTexture;
	private ImageTexture forestMountainTexture;
	private ImageTexture jungleMountainTexture;
	private ImageTexture hillsTexture;
	private ImageTexture forestHillsTexture;
	private ImageTexture jungleHillsTexture;
	private ImageTexture volcanosTexture;
	private ImageTexture forestVolcanoTexture;
	private ImageTexture jungleVolcanoTexture;

	public HillsLayer() {
		mountainTexture = Util.LoadTextureFromPCX("Art/Terrain/Mountains.pcx");
		snowMountainTexture = Util.LoadTextureFromPCX("Art/Terrain/Mountains-snow.pcx");
		forestMountainTexture = Util.LoadTextureFromPCX("Art/Terrain/mountain forests.pcx");
		jungleMountainTexture = Util.LoadTextureFromPCX("Art/Terrain/mountain jungles.pcx");
		hillsTexture = Util.LoadTextureFromPCX("Art/Terrain/xhills.pcx");
		forestHillsTexture = Util.LoadTextureFromPCX("Art/Terrain/hill forests.pcx");
		jungleHillsTexture = Util.LoadTextureFromPCX("Art/Terrain/hill jungle.pcx");
		volcanosTexture = Util.LoadTextureFromPCX("Art/Terrain/Volcanos.pcx");
		forestVolcanoTexture = Util.LoadTextureFromPCX("Art/Terrain/Volcanos forests.pcx");
		jungleVolcanoTexture = Util.LoadTextureFromPCX("Art/Terrain/Volcanos jungles.pcx");
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		if (tile.overlayTerrainType.isHilly()) {
			int pcxIndex = getMountainIndex(tile);
			int row = pcxIndex/4;
			int column = pcxIndex % 4;
			if (tile.overlayTerrainType.Key == "mountains") {
				Rect2 mountainRectangle = new Rect2(column * mountainSize.X, row * mountainSize.Y, mountainSize);
				Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * mountainSize + new Vector2(0, -12), mountainSize);
				ImageTexture mountainGraphics;
				if (tile.isSnowCapped) {
					mountainGraphics = snowMountainTexture;
				}
				else {
					TerrainType dominantVegetation = getDominantVegetationNearHillyTile(tile);
					if (dominantVegetation.Key == "forest") {
						mountainGraphics = forestMountainTexture;
					}
					else if (dominantVegetation.Key == "jungle") {
						mountainGraphics = jungleMountainTexture;
					}
					else {
						mountainGraphics = mountainTexture;
					}
				}
				looseView.DrawTextureRectRegion(mountainGraphics, screenTarget, mountainRectangle);
			}
			else if (tile.overlayTerrainType.Key == "hills") {
				Rect2 hillsRectangle = new Rect2(column * hillsSize.X, row * hillsSize.Y, hillsSize);
				Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * hillsSize + new Vector2(0, -4), hillsSize);
				ImageTexture hillGraphics;
				TerrainType dominantVegetation = getDominantVegetationNearHillyTile(tile);
				if (dominantVegetation.Key == "forest") {
					hillGraphics = forestHillsTexture;
				}
				else if (dominantVegetation.Key == "jungle") {
					hillGraphics = jungleHillsTexture;
				}
				else {
					hillGraphics = hillsTexture;
				}
				looseView.DrawTextureRectRegion(hillGraphics, screenTarget, hillsRectangle);
			}
			else if (tile.overlayTerrainType.Key == "volcano") {
				Rect2 volcanoRectangle = new Rect2(column * volcanoSize.X, row * volcanoSize.Y, volcanoSize);
				Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * volcanoSize + new Vector2(0, -12), volcanoSize);
				ImageTexture volcanoGraphics;
				TerrainType dominantVegetation = getDominantVegetationNearHillyTile(tile);
				if (dominantVegetation.Key == "forest") {
					volcanoGraphics = forestVolcanoTexture;
				}
				else if (dominantVegetation.Key == "jungle") {
					volcanoGraphics = jungleVolcanoTexture;
				}
				else {
					volcanoGraphics = volcanosTexture;
				}
				looseView.DrawTextureRectRegion(volcanoGraphics, screenTarget, volcanoRectangle);
			}
		}
	}

	private TerrainType getDominantVegetationNearHillyTile(Tile center)
	{
		TerrainType northeastType = center.neighbors[TileDirection.NORTHEAST].overlayTerrainType;
		TerrainType northwestType = center.neighbors[TileDirection.NORTHWEST].overlayTerrainType;
		TerrainType southeastType = center.neighbors[TileDirection.SOUTHEAST].overlayTerrainType;
		TerrainType southwestType = center.neighbors[TileDirection.SOUTHWEST].overlayTerrainType;

		TerrainType[] neighborTerrains = { northeastType, northwestType, southeastType, southwestType };

		int hills = 0;
		int forests = 0;
		int jungles = 0;
		//These references are so we can return the appropriate type, and because we don't have a good way
		//to grab them directly at this point in time.
		TerrainType forest = null;
		TerrainType jungle = null;
		foreach (TerrainType type in neighborTerrains) {
			if (type.isHilly()) {
				hills++;
			}
			else if (type.Key == "forest") {
				forests++;
				forest = type;
			}
			else if (type.Key == "jungle") {
				jungles++;
				jungle = type;
			}
		}

		if (hills + forests + jungles < 4) {	//some surrounding tiles are neither forested nor hilly
			return TerrainType.NONE;
		}
		if (forests == 0 && jungles == 0) {
			return TerrainType.NONE;	//all hills
		}
		if (forests > jungles) {
			return forest;
		}
		if (jungles > forests) {
			return jungle;
		}

		//If we get here, it's a tie between forest and jungle.  Deterministically choose one so it doesn't change on every render
		if (center.xCoordinate % 2 == 0) {
			return forest;
		}
		return jungle;
	}

	private int getMountainIndex(Tile tile) {
		int index = 0;
		if (tile.neighbors[TileDirection.NORTHWEST].overlayTerrainType.isHilly()) {
			index++;
		}
		if (tile.neighbors[TileDirection.NORTHEAST].overlayTerrainType.isHilly()) {
			index+=2;
		}
		if (tile.neighbors[TileDirection.SOUTHWEST].overlayTerrainType.isHilly()) {
			index+=4;
		}
		if (tile.neighbors[TileDirection.SOUTHEAST].overlayTerrainType.isHilly()) {
			index+=8;
		}
		return index;
	}
}

public partial class ForestLayer : LooseLayer {
	public static readonly Vector2 forestJungleSize = new Vector2(128, 88);

	private ImageTexture largeJungleTexture;
	private ImageTexture smallJungleTexture;
	private ImageTexture largeForestTexture;
	private ImageTexture largePlainsForestTexture;
	private ImageTexture largeTundraForestTexture;
	private ImageTexture smallForestTexture;
	private ImageTexture smallPlainsForestTexture;
	private ImageTexture smallTundraForestTexture;
	private ImageTexture pineForestTexture;
	private ImageTexture pinePlainsTexture;
	private ImageTexture pineTundraTexture;

	public ForestLayer() {
		largeJungleTexture       = Util.LoadTextureFromPCX("Art/Terrain/grassland forests.pcx", 0,   0, 512, 176);
		smallJungleTexture       = Util.LoadTextureFromPCX("Art/Terrain/grassland forests.pcx", 0, 176, 768, 176);
		largeForestTexture       = Util.LoadTextureFromPCX("Art/Terrain/grassland forests.pcx", 0, 352, 512, 176);
		largePlainsForestTexture = Util.LoadTextureFromPCX("Art/Terrain/plains forests.pcx",    0, 352, 512, 176);
		largeTundraForestTexture = Util.LoadTextureFromPCX("Art/Terrain/tundra forests.pcx",    0, 352, 512, 176);
		smallForestTexture       = Util.LoadTextureFromPCX("Art/Terrain/grassland forests.pcx", 0, 528, 640, 176);
		smallPlainsForestTexture = Util.LoadTextureFromPCX("Art/Terrain/plains forests.pcx",    0, 528, 640, 176);
		smallTundraForestTexture = Util.LoadTextureFromPCX("Art/Terrain/tundra forests.pcx",    0, 528, 640, 176);
		pineForestTexture        = Util.LoadTextureFromPCX("Art/Terrain/grassland forests.pcx", 0, 704, 768, 176);
		pinePlainsTexture        = Util.LoadTextureFromPCX("Art/Terrain/plains forests.pcx"   , 0, 704, 768, 176);
		pineTundraTexture        = Util.LoadTextureFromPCX("Art/Terrain/tundra forests.pcx"   , 0, 704, 768, 176);
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
		if (tile.overlayTerrainType.Key == "jungle") {
			//Randomly, but predictably, choose a large jungle graphic
			//More research is needed on when to use large vs small jungles.  Probably, small is used when neighboring fewer jungles.
			//For the first pass, we're just always using large jungles.
			int randomJungleRow = tile.yCoordinate % 2;
			int randomJungleColumn;
			ImageTexture jungleTexture;
			if (tile.getEdgeNeighbors().Any(t => t.IsWater())) {
				randomJungleColumn = tile.xCoordinate % 6;
				jungleTexture = smallJungleTexture;
			}
			else {
				randomJungleColumn = tile.xCoordinate % 4;
				jungleTexture = largeJungleTexture;
			}
			Rect2 jungleRectangle = new Rect2(randomJungleColumn * forestJungleSize.X, randomJungleRow * forestJungleSize.Y, forestJungleSize);
			Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * forestJungleSize + new Vector2(0, -12), forestJungleSize);
			looseView.DrawTextureRectRegion(jungleTexture, screenTarget, jungleRectangle);
		}
		if (tile.overlayTerrainType.Key == "forest") {
			int forestRow = 0;
			int forestColumn = 0;
			ImageTexture forestTexture;
			if (tile.isPineForest) {
				forestRow = tile.yCoordinate % 2;
				forestColumn = tile.xCoordinate % 6;
				if (tile.baseTerrainType.Key == "grassland") {
					forestTexture = pineForestTexture;
				}
				else if (tile.baseTerrainType.Key == "plains") {
					forestTexture = pinePlainsTexture;
				}
				else { //Tundra
					forestTexture = pineTundraTexture;
				}
			}
			else {
				forestRow = tile.yCoordinate % 2;
				if (tile.getEdgeNeighbors().Any(t => t.IsWater())) {
					forestColumn = tile.xCoordinate % 5;
					if (tile.baseTerrainType.Key == "grassland") {
						forestTexture = smallForestTexture;
					}
					else if (tile.baseTerrainType.Key == "plains") {
						forestTexture = smallPlainsForestTexture;
					}
					else {	//tundra
						forestTexture = smallTundraForestTexture;
					}
				}
				else {
					forestColumn = tile.xCoordinate % 4;
					if (tile.baseTerrainType.Key == "grassland") {
						forestTexture = largeForestTexture;
					}
					else if (tile.baseTerrainType.Key == "plains") {
						forestTexture = largePlainsForestTexture;
					}
					else {	//tundra
						forestTexture = largeTundraForestTexture;
					}
				}
			}
			Rect2 forestRectangle = new Rect2(forestColumn * forestJungleSize.X, forestRow * forestJungleSize.Y, forestJungleSize);
			Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * forestJungleSize + new Vector2(0, -12), forestJungleSize);
			looseView.DrawTextureRectRegion(forestTexture, screenTarget, forestRectangle);
		}
	}
}
public partial class MarshLayer : LooseLayer {
	public static readonly Vector2 marshSize = new Vector2(128, 88);
	//Because the marsh graphics are 88 pixels tall instead of the 64 of a tile, we also need an addition 12 pixel offset to the top
	//88 - 64 = 24; 24/2 = 12.  This keeps the marsh centered with half the extra 24 pixels above the tile and half below.
	readonly Vector2 MARSH_OFFSET = (float)0.5 * marshSize + new Vector2(0, -12);

	private ImageTexture largeMarshTexture;
	private ImageTexture smallMarshTexture;

	public MarshLayer() {
		largeMarshTexture = Util.LoadTextureFromPCX("Art/Terrain/marsh.pcx", 0,   0, 512, 176);
		smallMarshTexture = Util.LoadTextureFromPCX("Art/Terrain/marsh.pcx", 0, 176, 640, 176);
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
		if (tile.overlayTerrainType.Key == "marsh") {
			int randomJungleRow = tile.yCoordinate % 2;
			int randomMarshColumn;
			ImageTexture marshTexture;
			if (tile.getEdgeNeighbors().Any(t => t.IsWater())) {
				randomMarshColumn = tile.xCoordinate % 5;
				marshTexture = smallMarshTexture;
			}
			else {
				randomMarshColumn = tile.xCoordinate % 4;
				marshTexture = largeMarshTexture;
			}
			Rect2 jungleRectangle = new Rect2(randomMarshColumn * marshSize.X, randomJungleRow * marshSize.Y, marshSize);
			Rect2 screenTarget = new Rect2(tileCenter - MARSH_OFFSET, marshSize);
			looseView.DrawTextureRectRegion(marshTexture, screenTarget, jungleRectangle);
		}
	}
}

public partial class RiverLayer : LooseLayer
{
	public static readonly Vector2 riverSize = new Vector2(128, 64);
	public static readonly Vector2 riverCenterOffset = new Vector2(riverSize.X / 2, 0);
	private ImageTexture riverTexture;

	public RiverLayer() {
		riverTexture = Util.LoadTextureFromPCX("Art/Terrain/mtnRivers.pcx");
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		//The "point" is the easternmost point of the tile for which we are drawing rivers.
		//Which river graphics to used is calculated by evaluating the tiles that neighbor
		//that point.
		Tile northOfPoint = tile.neighbors[TileDirection.NORTHEAST];
		Tile eastOfPoint = tile.neighbors[TileDirection.EAST];
		Tile westOfPoint = tile;
		Tile southOfPoint = tile.neighbors[TileDirection.SOUTHEAST];

		int riverGraphicsIndex = 0;

		if (northOfPoint.riverSouthwest) {
			riverGraphicsIndex++;
		}
		if (eastOfPoint.riverNorthwest) {
			riverGraphicsIndex+=2;
		}
		if (westOfPoint.riverSoutheast) {
			riverGraphicsIndex+=4;
		}
		if (southOfPoint.riverNortheast) {
			riverGraphicsIndex+=8;
		}
		if (riverGraphicsIndex == 0) {
			return;
		}
		int riverRow = riverGraphicsIndex / 4;
		int riverColumn = riverGraphicsIndex % 4;

		Rect2 riverRectangle = new Rect2(riverColumn * riverSize.X, riverRow * riverSize.Y, riverSize);
		Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * riverSize + riverCenterOffset, riverSize);
		looseView.DrawTextureRectRegion(riverTexture, screenTarget, riverRectangle);
	}
}

public partial class GridLayer : LooseLayer {
	public Color color = Color.Color8(50, 50, 50, 150);
	public float lineWidth = (float)1.0;

	public GridLayer() {}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		Vector2 cS = MapView.cellSize;
		Vector2 left  = tileCenter + new Vector2(-cS.X, 0    );
		Vector2 top   = tileCenter + new Vector2( 0   , -cS.Y);
		Vector2 right = tileCenter + new Vector2( cS.X, 0    );
		looseView.DrawLine(left, top  , color, lineWidth);
		looseView.DrawLine(top , right, color, lineWidth);
	}
}

public partial class BuildingLayer : LooseLayer {
	private ImageTexture buildingsTex;
	private Vector2 buildingSpriteSize;

	public BuildingLayer()
	{
		var buildingsPCX = new Pcx(Util.Civ3MediaPath("Art/Terrain/TerrainBuildings.PCX"));
		buildingsTex = PCXToGodot.getImageTextureFromPCX(buildingsPCX);
		//In Conquests, this graphic is 4x4, and the search path will now find the Conquests one first
		buildingSpriteSize = new Vector2((float)buildingsTex.GetWidth() / 4, (float)buildingsTex.GetHeight() / 4);
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		if (tile.hasBarbarianCamp) {
			var texRect = new Rect2(buildingSpriteSize * new Vector2 (2, 0), buildingSpriteSize); //(2, 0) is the offset in the TerrainBuildings.PCX file (top row, third in)
			// TODO: Modify this calculation so it doesn't assume buildingSpriteSize is the same as the size of the terrain tiles
			var screenRect = new Rect2(tileCenter - (float)0.5 * buildingSpriteSize, buildingSpriteSize);
			looseView.DrawTextureRectRegion(buildingsTex, screenRect, texRect);
		}
	}
}

public partial class LooseView : Node2D {
	public MapView mapView;
	public List<LooseLayer> layers = new List<LooseLayer>();

	public LooseView(MapView mapView)
	{
		this.mapView = mapView;
		SetPremultAlpha();
	}

	public void SetPremultAlpha() {
		// Use premultiplied alpha blending to prevent magenta lines along terrain borders
		// and hill/mountain outlines. Might change in the future if this is not the desired
		// blending behaviour for all LooseView instances.
		CanvasItemMaterial material = new CanvasItemMaterial();
		this.Material = material;
		material.BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha;
	}

	private struct VisibleTile
	{
		public Tile tile;
		public Vector2 tileCenter;
	}

	public override void _Draw()
	{
		base._Draw();

		using (var gameDataAccess = new UIGameDataAccess()) {
			GameData gD = gameDataAccess.gameData;

			// Iterating over visible tiles is unfortunately pretty expensive. Assemble a list of Tile references and centers first so we don't
			// have to reiterate for each layer. Doing this improves framerate significantly.
			MapView.VisibleRegion visRegion = mapView.getVisibleRegion();
			List<VisibleTile> visibleTiles = new List<VisibleTile>();
			for (int y = visRegion.upperLeftY; y < visRegion.lowerRightY; y++) {
				if (gD.map.isRowAt(y)) {
					for (int x = visRegion.getRowStartX(y); x < visRegion.lowerRightX; x += 2) {
						Tile tile = gD.map.tileAt(x, y);
						if (IsTileKnown(tile, gameDataAccess)) {
							visibleTiles.Add(new VisibleTile { tile = tile, tileCenter = MapView.cellSize * new Vector2(x + 1, y + 1) });
						}
					}
				}
			}

			foreach (LooseLayer layer in layers.FindAll(L => L.visible && !(L is FogOfWarLayer))) {
				layer.onBeginDraw(this, gD);
				foreach (VisibleTile vT in visibleTiles) {
					layer.drawObject(this, gD, vT.tile, vT.tileCenter);
				}
				layer.onEndDraw(this, gD);
			}

			if (!gD.observerMode) {
				foreach (LooseLayer layer in layers.FindAll(layer => layer is FogOfWarLayer)) {
					for (int y = visRegion.upperLeftY; y < visRegion.lowerRightY; y++)
						if (gD.map.isRowAt(y))
							for (int x = visRegion.getRowStartX(y); x < visRegion.lowerRightX; x += 2) {
								Tile tile = gD.map.tileAt(x, y);
								if (tile != Tile.NONE) {
									VisibleTile invisibleTile = new VisibleTile { tile = tile, tileCenter = MapView.cellSize * new Vector2(x + 1, y + 1) };
									layer.drawObject(this, gD, tile, invisibleTile.tileCenter);
								}
							}
				}
			}
		}
	}
	private static bool IsTileKnown(Tile tile, UIGameDataAccess gameDataAccess) {
		if (gameDataAccess.gameData.observerMode) {
			return true;
		}
		return tile != Tile.NONE && gameDataAccess.gameData.GetHumanPlayers()[0].tileKnowledge.isTileKnown(tile);
	}
}

public partial class MapView : Node2D {
	// cellSize is half the size of the tile sprites, or the amount of space each tile takes up when they are packed on the grid (note tiles are
	// staggered and half overlap).
	public static readonly Vector2 cellSize = new Vector2(64, 32);
	public Vector2 scaledCellSize {
		get { return cellSize * new Vector2(cameraZoom, cameraZoom); }
	}

	public Game game;

	public int mapWidth  { get; private set; }
	public int mapHeight { get; private set; }
	public bool wrapHorizontally { get; private set; }
	public bool wrapVertically   { get; private set; }

	private Vector2 internalCameraLocation = new Vector2(0, 0);
	public Vector2 cameraLocation {
		get {
			return internalCameraLocation;
		}
		set {
			setCameraLocation(value);
		}
	}
	public float internalCameraZoom = 1;
	public float cameraZoom {
		get { return internalCameraZoom; }
		set { setCameraZoomFromMiddle(value); }
	}

	private LooseView looseView;

	// Specifies a rectangular block of tiles that are currently potentially on screen. Accessible through getVisibleRegion(). Tile coordinates
	// are "virtual", i.e. "unwrapped", so there isn't necessarily a tile at each location. The region is intended to include the upper left
	// coordinates but not the lower right ones. When iterating over all tiles in the region you must account for the fact that map rows are
	// staggered, see LooseView._Draw for an example.
	public struct VisibleRegion {
		public int upperLeftX, upperLeftY;
		public int lowerRightX, lowerRightY;

		public int getRowStartX(int y)
		{
			return upperLeftX + (y - upperLeftY)%2;
		}
	}

	public GridLayer gridLayer { get; private set; }

	public ImageTexture civColorWhitePalette = null;

	public MapView(Game game, int mapWidth, int mapHeight, bool wrapHorizontally, bool wrapVertically)
	{
		this.game = game;
		this.mapWidth = mapWidth;
		this.mapHeight = mapHeight;
		this.wrapHorizontally = wrapHorizontally;
		this.wrapVertically = wrapVertically;

		looseView = new LooseView(this);
		looseView.layers.Add(new TerrainLayer());
		looseView.layers.Add(new RiverLayer());
		looseView.layers.Add(new ForestLayer());
		looseView.layers.Add(new MarshLayer());
		looseView.layers.Add(new HillsLayer());
		looseView.layers.Add(new TntLayer());
		looseView.layers.Add(new RoadLayer());
		looseView.layers.Add(new ResourceLayer());
		this.gridLayer = new GridLayer();
		looseView.layers.Add(this.gridLayer);
		looseView.layers.Add(new BuildingLayer());
		looseView.layers.Add(new UnitLayer());
		looseView.layers.Add(new CityLayer());
		looseView.layers.Add(new FogOfWarLayer());

		(civColorWhitePalette, _) = Util.loadPalettizedPCX("Art/Units/Palettes/ntp00.pcx");

		AddChild(looseView);
	}

	public override void _Process(double delta)
	{
		// Redraw everything. This is necessary so that animations play. Maybe we could only update the unit layer but long term I think it's
		// better to redraw everything every frame like a typical modern video game.
		looseView.QueueRedraw();
	}

	// Returns the size in pixels of the area in which the map will be drawn. This is the viewport size or, if that's null, the window size.
	public Vector2 getVisibleAreaSize()
	{
		return GetViewport() != null ? GetViewportRect().Size : DisplayServer.WindowGetSize();
	}

	public VisibleRegion getVisibleRegion()
	{
		(int x0, int y0) = tileCoordsOnScreenAt(new Vector2(0, 0));
		Vector2 mapViewSize = new Vector2(2, 4) + getVisibleAreaSize() / scaledCellSize;
		return new VisibleRegion { upperLeftX = x0 - 2, upperLeftY = y0 - 2,
			lowerRightX = x0 + (int)mapViewSize.X, lowerRightY = y0 + (int)mapViewSize.Y };
	}

	// "center" is the screen location around which the zoom is centered, e.g., if center is (0, 0) the tile in the top left corner will be the
	// same after the zoom level is changed, and if center is screenSize/2, the tile in the center of the window won't change.
	public void setCameraZoom(float newScale, Vector2 center)
	{
		var v2NewZoom = new Vector2(newScale, newScale);
		var v2OldZoom = new Vector2(cameraZoom, cameraZoom);
		if (v2NewZoom != v2OldZoom) {
			internalCameraZoom = newScale;
			looseView.Scale = v2NewZoom;
			setCameraLocation ((v2NewZoom / v2OldZoom) * (cameraLocation + center) - center);
		}
	}

	// Zooms in or out centered on the middle of the screen
	public void setCameraZoomFromMiddle(float newScale)
	{
		setCameraZoom(newScale, getVisibleAreaSize() / 2);
	}

	public void moveCamera(Vector2 offset)
	{
		setCameraLocation(cameraLocation + offset);
	}

	public void setCameraLocation(Vector2 location)
	{
		// Prevent the camera from moving beyond an unwrapped edge of the map. One complication here is that the viewport might actually be
		// larger than the map (if we're zoomed far out) so in that case we must apply the constraint the other way around, i.e. constrain the
		// map to the viewport rather than the viewport to the map.
		Vector2 visAreaSize = getVisibleAreaSize();
		Vector2 mapPixelSize = new Vector2(cameraZoom, cameraZoom) * (new Vector2(cellSize.X * (mapWidth + 1), cellSize.Y * (mapHeight + 1)));
		if (!wrapHorizontally) {
			float leftLim, rightLim;
			{
				if (mapPixelSize.X >= visAreaSize.X) {
					leftLim = 0;
					rightLim = mapPixelSize.X - visAreaSize.X;
				} else {
					leftLim = mapPixelSize.X - visAreaSize.X;
					rightLim = 0;
				}
			}
			if (location.X < leftLim)
				location.X = leftLim;
			else if (location.X > rightLim)
				location.X = rightLim;
		}
		if (!wrapVertically) {
			// These margins allow the player to move the camera that far off those map edges so that the UI controls don't cover up the
			// map. TODO: These values should be read from the sizes of the UI elements instead of hardcoded.
			float topMargin = 70, bottomMargin = 140;
			float topLim, bottomLim;
			{
				if (mapPixelSize.Y >= visAreaSize.Y) {
					topLim = -topMargin;
					bottomLim = mapPixelSize.Y - visAreaSize.Y + bottomMargin;
				} else {
					topLim = mapPixelSize.Y - visAreaSize.Y;
					bottomLim = 0;
				}
			}
			if (location.Y < topLim)
				location.Y = topLim;
			else if (location.Y > bottomLim)
				location.Y = bottomLim;
		}

		internalCameraLocation = location;
		looseView.Position = -location;
	}

	public Vector2 screenLocationOfTileCoords(int x, int y, bool center = true)
	{
		// Add one to x & y to get the tile center b/c in Civ 3 the tile at (x, y) is a diamond centered on (x+1, y+1).
		Vector2 centeringOffset = center ? new Vector2(1, 1) : new Vector2(0, 0);

		var mapLoc = (new Vector2(x, y) + centeringOffset) * cellSize;
		return mapLoc * cameraZoom - cameraLocation;
	}

	// Returns the location of tile (x, y) on the screen, if "center" is true returns the location of the tile center and otherwise returns the
	// upper left. Works even if (x, y) is off screen or out of bounds.
	public Vector2 screenLocationOfTile(Tile tile, bool center = true)
	{
		return screenLocationOfTileCoords(tile.xCoordinate, tile.yCoordinate, center);
	}

	// Returns the virtual tile coordinates on screen at the given location. "Virtual" meaning the coordinates are unwrapped and there isn't
	// necessarily a tile there at all.
	public (int, int) tileCoordsOnScreenAt(Vector2 screenLocation)
	{
		Vector2 mapLoc = (screenLocation + cameraLocation) / scaledCellSize;
		Vector2 intMapLoc = mapLoc.Floor();
		Vector2 fracMapLoc = mapLoc - intMapLoc;
		int x = (int)intMapLoc.X, y = (int)intMapLoc.Y;
		bool evenColumn = x%2 == 0, evenRow = y%2 == 0;
		if (evenColumn ^ evenRow) {
			if (fracMapLoc.Y > fracMapLoc.X)
				x -= 1;
			else
				y -= 1;
		} else {
			if (fracMapLoc.Y < 1 - fracMapLoc.X) {
				x -= 1;
				y -= 1;
			}
		}
		return (x, y);
	}

	public Tile tileOnScreenAt(GameMap map, Vector2 screenLocation)
	{
		(int x, int y) = tileCoordsOnScreenAt(screenLocation);
		return map.tileAt(x, y);
	}

	public void centerCameraOnTile(Tile t)
	{
		var tileCenter = new Vector2(t.xCoordinate + 1, t.yCoordinate + 1) * scaledCellSize;
		setCameraLocation(tileCenter - (float)0.5 * getVisibleAreaSize());
	}
}
