using System.Collections.Generic;
using System;
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

public class TerrainLayer : LooseLayer {

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
			return this.tile.ExtraInfo.BaseTerrainFileID.CompareTo(other?.tile.ExtraInfo.BaseTerrainFileID);
		}
	}

	private List<TileToDraw> tilesToDraw = new List<TileToDraw>();

	public TerrainLayer()
	{
		tripleSheets = loadTerrainTripleSheets();
	}

	public List<ImageTexture> loadTerrainTripleSheets()
	{
		var fileNames = new List<string> {
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
		var tr = new List<ImageTexture>();
		foreach (var fileName in fileNames)
			tr.Add(Util.LoadTextureFromPCX(fileName));
		return tr;
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
			int xSheet = tTD.tile.ExtraInfo.BaseTerrainImageID % 9, ySheet = tTD.tile.ExtraInfo.BaseTerrainImageID / 9;
			Rect2 texRect = new Rect2(new Vector2(xSheet, ySheet) * terrainSpriteSize, terrainSpriteSize);
			Vector2 terrainOffset = new Vector2(0, -1 * MapView.cellSize.y);
			Rect2 screenRect = new Rect2(tTD.tileCenter - (float)0.5 * terrainSpriteSize + terrainOffset, terrainSpriteSize);
			looseView.DrawTextureRectRegion(tripleSheets[tTD.tile.ExtraInfo.BaseTerrainFileID], screenRect, texRect);
		}
		tilesToDraw.Clear();
	}
}

public class HillsLayer : LooseLayer {
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
				Rect2 mountainRectangle = new Rect2(column * mountainSize.x, row * mountainSize.y, mountainSize);
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
				Rect2 hillsRectangle = new Rect2(column * hillsSize.x, row * hillsSize.y, hillsSize);
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
				Rect2 volcanoRectangle = new Rect2(column * volcanoSize.x, row * volcanoSize.y, volcanoSize);
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

public class ForestLayer : LooseLayer {
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
			if (tile.NeighborsWater()) {
				randomJungleColumn = tile.xCoordinate % 6;
				jungleTexture = smallJungleTexture;
			}
			else {
				randomJungleColumn = tile.xCoordinate % 4;
				jungleTexture = largeJungleTexture;
			}
			Rect2 jungleRectangle = new Rect2(randomJungleColumn * forestJungleSize.x, randomJungleRow * forestJungleSize.y, forestJungleSize);
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
				if (tile.NeighborsWater()) {
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
			Rect2 forestRectangle = new Rect2(forestColumn * forestJungleSize.x, forestRow * forestJungleSize.y, forestJungleSize);
			Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * forestJungleSize + new Vector2(0, -12), forestJungleSize);
			looseView.DrawTextureRectRegion(forestTexture, screenTarget, forestRectangle);
		}
	}
}
public class MarshLayer : LooseLayer {
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
			if (tile.NeighborsWater()) {
				randomMarshColumn = tile.xCoordinate % 5;
				marshTexture = smallMarshTexture;
			}
			else {
				randomMarshColumn = tile.xCoordinate % 4;
				marshTexture = largeMarshTexture;
			}
			Rect2 jungleRectangle = new Rect2(randomMarshColumn * marshSize.x, randomJungleRow * marshSize.y, marshSize);
			Rect2 screenTarget = new Rect2(tileCenter - MARSH_OFFSET, marshSize);
			looseView.DrawTextureRectRegion(marshTexture, screenTarget, jungleRectangle);
		}
	}
}

public class RiverLayer : LooseLayer
{
	public static readonly Vector2 riverSize = new Vector2(128, 64);
	public static readonly Vector2 riverCenterOffset = new Vector2(riverSize.x / 2, 0);
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

		Rect2 riverRectangle = new Rect2(riverColumn * riverSize.x, riverRow * riverSize.y, riverSize);
		Rect2 screenTarget = new Rect2(tileCenter - (float)0.5 * riverSize + riverCenterOffset, riverSize);
		looseView.DrawTextureRectRegion(riverTexture, screenTarget, riverRectangle);
	}
}

public class GridLayer : LooseLayer {
	public Color color = Color.Color8(50, 50, 50, 150);
	public float lineWidth = (float)1.0;

	public GridLayer() {}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		var cS = MapView.cellSize;
		var left  = tileCenter + new Vector2(-cS.x,  0   );
		var top   = tileCenter + new Vector2( 0   , -cS.y);
		var right = tileCenter + new Vector2( cS.x,  0   );
		looseView.DrawLine(left, top  , color, lineWidth);
		looseView.DrawLine(top , right, color, lineWidth);
	}
}

public class UnitLayer : LooseLayer {
	private ImageTexture unitIcons;
	private int unitIconsWidth;
	private ImageTexture unitMovementIndicators;

	// The unit animations, effect animations, and cursor are all drawn as children attached to the looseView but aren't created and attached in
	// any particular order so we must use the ZIndex property to ensure they're properly layered.
	public const int effectAnimZIndex = 150;
	public const int unitAnimZIndex = 100;
	public const int cursorZIndex = 50;

	public UnitLayer()
	{
		var iconPCX = new Pcx(Util.Civ3MediaPath("Art/Units/units_32.pcx"));
		unitIcons = PCXToGodot.getImageTextureFromPCX(iconPCX);
		unitIconsWidth = (unitIcons.GetWidth() - 1) / 33;

		var moveIndPCX = new Pcx(Util.Civ3MediaPath("Art/interface/MovementLED.pcx"));
		unitMovementIndicators = PCXToGodot.getImageTextureFromPCX(moveIndPCX);
	}

	// Creates a quad mesh with the given shader attached. The quad is 1.0 units long on both sides, intended to be scaled to the appropriate size
	// when used.
	public static (ShaderMaterial, MeshInstance2D) createShadedQuad(Shader shader)
	{
		var quad = new QuadMesh();
		quad.Size = new Vector2(1, 1);

		var shaderMat = new ShaderMaterial();
		shaderMat.Shader = shader;

		var meshInst = new MeshInstance2D();
		meshInst.Material = shaderMat;
		meshInst.Mesh = quad;

		return (shaderMat, meshInst);
	}

	public Color getHPColor(float fractionRemaining)
	{
		if (fractionRemaining >= (float)0.67)
			return Color.Color8(0, 255, 0);
		else if (fractionRemaining >= (float)0.34)
			return Color.Color8(255, 255, 0);
		else
			return Color.Color8(255, 0, 0);
	}

	// AnimationInstance represents an animation appearing on the screen. It's specific to a unit, action, and direction. AnimationInstances have
	// two components: a ShaderMaterial and a MeshInstance2D. The ShaderMaterial runs the unit shader (created by UnitLayer.getShader) with all
	// the parameters set to a particular texture, civ color, direction, etc. The MeshInstance2D is what's actually drawn by Godot, i.e., what's
	// added to the node tree. AnimationInstances are only active for one frame at a time but they live as long as the UnitLayer. They are
	// retrieved or created as needed by getBlankAnimationInstance during the drawing of units and are hidden & requeued for use at the beginning
	// of each frame.
	public class AnimationInstance {
		public ShaderMaterial shaderMat;
		public MeshInstance2D meshInst;

		public AnimationInstance(LooseView looseView)
		{
			(shaderMat, meshInst) = createShadedQuad(getUnitShader());
			shaderMat.SetShaderParam("civColorWhitePalette", looseView.mapView.civColorWhitePalette);

			looseView.AddChild(meshInst);
			meshInst.Hide();
		}
	}

	private List<AnimationInstance> animInsts = new List<AnimationInstance>();
	private int nextBlankAnimInst = 0;

	// Returns the next unused AnimationInstance or creates & returns a new one if none are available.
	public AnimationInstance getBlankAnimationInstance(LooseView looseView)
	{
		if (nextBlankAnimInst >= animInsts.Count) {
			animInsts.Add(new AnimationInstance(looseView));
		}
		var tr = animInsts[nextBlankAnimInst];
		nextBlankAnimInst++;
		tr.meshInst.Show();
		return tr;
	}

	// Sets the palette, indices, relSpriteSize, and spriteXY parameters on a ShaderMaterial to pick a sprite from a FlicSheet. relativeColumn
	// varies between 0.0 for the first column and 1.0 for the last one.
	public static void setFlicShaderParams(ShaderMaterial mat, Util.FlicSheet flicSheet, int row, float relativeColumn)
	{
		mat.SetShaderParam("palette", flicSheet.palette);
		mat.SetShaderParam("indices", flicSheet.indices);

		var indicesDims = new Vector2(flicSheet.indices.GetWidth(), flicSheet.indices.GetHeight());
		var spriteSize = new Vector2(flicSheet.spriteWidth, flicSheet.spriteHeight);
		mat.SetShaderParam("relSpriteSize", spriteSize / indicesDims);

		int spritesPerRow = flicSheet.indices.GetWidth() / flicSheet.spriteWidth;
		int spriteColumn = (int)(relativeColumn * spritesPerRow);
		if (spriteColumn >= spritesPerRow)
			spriteColumn = spritesPerRow - 1;
		else if (spriteColumn < 0)
			spriteColumn = 0;
		mat.SetShaderParam("spriteXY", new Vector2(spriteColumn, row));
	}

	public void drawUnitAnimFrame(LooseView looseView, MapUnit unit, MapUnit.Appearance appearance, Vector2 tileCenter)
	{
		var flicSheet = looseView.mapView.game.civ3AnimData.forUnit(unit.unitType.name, appearance.action).getFlicSheet();

		int dirIndex = 0;
		switch (appearance.direction) {
		case TileDirection.NORTH:     dirIndex = 5; break;
		case TileDirection.NORTHEAST: dirIndex = 4; break;
		case TileDirection.EAST:      dirIndex = 3; break;
		case TileDirection.SOUTHEAST: dirIndex = 2; break;
		case TileDirection.SOUTH:     dirIndex = 1; break;
		case TileDirection.SOUTHWEST: dirIndex = 0; break;
		case TileDirection.WEST:      dirIndex = 7; break;
		case TileDirection.NORTHWEST: dirIndex = 6; break;
		}

		var animOffset = MapView.cellSize * new Vector2(appearance.offsetX, appearance.offsetY);
		// Need to move the sprites upward a bit so that their feet are at the center of the tile. I don't know if spriteHeight/4 is the right
		var position = tileCenter + animOffset - new Vector2(0, flicSheet.spriteHeight / 4);

		var inst = getBlankAnimationInstance(looseView);

		setFlicShaderParams(inst.shaderMat, flicSheet, dirIndex, appearance.progress);
		var civColor = new Color(unit.owner.color);
		inst.shaderMat.SetShaderParam("civColor", new Vector3(civColor.r, civColor.g, civColor.b));

		inst.meshInst.Position = position;
		// Make y scale negative so the texture isn't drawn upside-down. TODO: Explain more
		inst.meshInst.Scale = new Vector2(flicSheet.spriteWidth, -1 * flicSheet.spriteHeight);
		inst.meshInst.ZIndex = unitAnimZIndex;
	}

	public void drawEffectAnimFrame(LooseView looseView, Civ3Anim anim, float progress, Vector2 tileCenter)
	{
		var flicSheet = anim.getFlicSheet();
		var inst = getBlankAnimationInstance(looseView);
		setFlicShaderParams(inst.shaderMat, flicSheet, 0, progress);
		inst.shaderMat.SetShaderParam("civColor", new Vector3(1, 1, 1));
		inst.meshInst.Position = tileCenter;
		inst.meshInst.Scale = new Vector2(flicSheet.spriteWidth, -1 * flicSheet.spriteHeight);
		inst.meshInst.ZIndex = effectAnimZIndex;
	}

	private Util.FlicSheet cursorFlicSheet;
	private ShaderMaterial cursorMat = null;
	private MeshInstance2D cursorMesh = null;

	public void drawCursor(LooseView looseView, Vector2 position)
	{
		// Initialize cursor if necessary
		if (cursorMesh == null) {
			(cursorFlicSheet, _) = Util.loadFlicSheet("Art/Animations/Cursor/Cursor.flc");
			(cursorMat, cursorMesh) = createShadedQuad(getCursorShader());
			cursorMesh.Scale = new Vector2(cursorFlicSheet.spriteWidth, -1 * cursorFlicSheet.spriteHeight);
			cursorMesh.ZIndex = cursorZIndex;
			looseView.AddChild(cursorMesh);
		}

		const double period = 2.5; // TODO: Just eyeballing this for now. Read the actual period from the INI or something.
		var repCount = (double)OS.GetTicksMsec() / 1000.0 / period;
		var progress = (float)(repCount - Math.Floor(repCount));

		setFlicShaderParams(cursorMat, cursorFlicSheet, 0, progress);
		cursorMesh.Position = position;
		cursorMesh.Show();
	}

	public override void onBeginDraw(LooseView looseView, GameData gameData)
	{
		// Reset animation instances
		for (int n = 0; n < nextBlankAnimInst; n++)
			animInsts[n].meshInst.Hide();
		nextBlankAnimInst = 0;

		// Hide cursor if it's been initialized
		if (cursorMesh != null)
			cursorMesh.Hide();

		looseView.mapView.game.updateAnimations(gameData);
	}

	// Returns which unit should be drawn from among a list of units. The list is assumed to be non-empty.
	public MapUnit selectUnitToDisplay(LooseView looseView, List<MapUnit> units)
	{
		// From the list, pick out which units are (1) the strongest defender vs the currently selected unit, (2) the currently selected unit
		// itself if it's in the list, and (3) any unit that is playing an animation that the player would want to see.
		MapUnit bestDefender = units[0],
			selected = null,
			doingInterestingAnimation = null;
		var currentlySelectedUnit = looseView.mapView.game.CurrentlySelectedUnit;
		foreach (var u in units) {
			if (u == currentlySelectedUnit)
				selected = u;
			if (u.HasPriorityAsDefender(bestDefender, currentlySelectedUnit))
				bestDefender = u;
			if (looseView.mapView.game.animTracker.getUnitAppearance(u).DeservesPlayerAttention())
				doingInterestingAnimation = u;
		}

		// Prefer showing the selected unit, secondly show one doing a relevant animation, otherwise show the top defender
		return selected != null ? selected : (doingInterestingAnimation != null ? doingInterestingAnimation : bestDefender);
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		// First draw animated effects. These will always appear over top of units regardless of draw order due to z-index.
		Civ3Anim tileEffect = looseView.mapView.game.animTracker.getTileEffect(tile);
		if (tileEffect != null) {
			(_, float progress) = looseView.mapView.game.animTracker.getCurrentActionAndProgress(tile);
			drawEffectAnimFrame(looseView, tileEffect, progress, tileCenter);
		}

		if (tile.unitsOnTile.Count == 0)
			return;

		var white = Color.Color8(255, 255, 255);

		var unit = selectUnitToDisplay(looseView, tile.unitsOnTile);
		var appearance = looseView.mapView.game.animTracker.getUnitAppearance(unit);
		var animOffset = new Vector2(appearance.offsetX, appearance.offsetY) * MapView.cellSize;

		// If the unit we're about to draw is currently selected, draw the cursor first underneath it
		if ((unit != MapUnit.NONE) && (unit == looseView.mapView.game.CurrentlySelectedUnit))
			drawCursor(looseView, tileCenter + animOffset);

		drawUnitAnimFrame(looseView, unit, appearance, tileCenter);

		Vector2 indicatorLoc = tileCenter - new Vector2(26, 40) + animOffset;

		int mp = unit.movementPointsRemaining;
		int moveIndIndex = (mp <= 0) ? 4 : ((mp >= unit.unitType.movement) ? 0 : 2);
		Vector2 moveIndUpperLeft = new Vector2(1 + 7 * moveIndIndex, 1);
		Rect2 moveIndRect = new Rect2(moveIndUpperLeft, new Vector2(6, 6));
		var screenRect = new Rect2(indicatorLoc, new Vector2(6, 6));
		looseView.DrawTextureRectRegion(unitMovementIndicators, screenRect, moveIndRect);

		int hpIndHeight = 6 * (unit.maxHitPoints <= 5 ? unit.maxHitPoints : 5),
			hpIndWidth = 6;
		var hpIndBackgroundRect = new Rect2(indicatorLoc + new Vector2(-1, 8), new Vector2(hpIndWidth, hpIndHeight));
		if ((unit.unitType.attack > 0) || (unit.unitType.defense > 0)) {
			float hpFraction = (float)unit.hitPointsRemaining / unit.maxHitPoints;
			looseView.DrawRect(hpIndBackgroundRect, Color.Color8(0, 0, 0));
			float hpHeight = hpFraction * (hpIndHeight - 2);
			if (hpHeight < 1)
				hpHeight = 1;
			var hpContentsRect = new Rect2(hpIndBackgroundRect.Position + new Vector2(1, hpIndHeight - 1 - hpHeight), // position
										   new Vector2(hpIndWidth - 2, hpHeight)); // size
			looseView.DrawRect(hpContentsRect, getHPColor(hpFraction));
			if (unit.isFortified)
				looseView.DrawRect(hpIndBackgroundRect, white, false);
		}

		// Draw lines to show that there are more units on this tile
		if (tile.unitsOnTile.Count > 1) {
			int lineCount = tile.unitsOnTile.Count;
			if (lineCount > 5)
				lineCount = 5;
			for (int n = 0; n < lineCount; n++) {
				var lineStart = indicatorLoc + new Vector2(-2, hpIndHeight + 12 + 4 * n);
				looseView.DrawLine(lineStart                    , lineStart + new Vector2(8, 0), white);
				looseView.DrawLine(lineStart + new Vector2(0, 1), lineStart + new Vector2(8, 1), Color.Color8(75, 75, 75));
			}
		}
	}

	private static Shader unitShader = null;

	public static Shader getUnitShader()
	{
		if (unitShader != null)
			return unitShader;

		// It would make more sense to use a usampler2D for the indices but that doesn't work. As far as I can tell, (u)int samplers are
		// broken on Godot because there's no way to create a texture with a compatible format. See:
		// https://www.khronos.org/opengl/wiki/Sampler_(GLSL)#Sampler_types - Says it's undefined behavior to read from a usampler2d if the
		// attached texture format is not GL_R8UI.
		// https://docs.godotengine.org/en/stable/classes/class_image.html#enum-image-format - None of the Godot texture formats correspond to
		// GL_R8UI. The closest is FORMAT_R8 which corresponds to GL_RED except that won't work since it's a floating point format.
		string shaderSource = @"
		shader_type canvas_item;

		uniform sampler2D palette;
		uniform sampler2D civColorWhitePalette;
		uniform sampler2D indices;
		uniform vec2 relSpriteSize; // sprite size relative to the entire sheet
		uniform vec2 spriteXY; // coordinates of the sprite to be drawn, in number of sprites not pixels
		uniform vec3 civColor;

		vec4 sampleCivTintedColor(vec2 paletteCoords)
		{
			return vec4(civColor, 1.0) * texture(civColorWhitePalette, paletteCoords);
		}

		void vertex()
		{
			UV = (spriteXY + UV) * relSpriteSize;
		}

		void fragment()
		{
			int colorIndex = int(255.0 * texture(indices, UV).r);
			if (colorIndex >= 254) // indices 254 and 255 are transparent
				discard;
			else if (colorIndex >= 240) // indices in [240, 253] are shadows
				COLOR = vec4(0.0, 0.0, 0.0, float(16 * (255 - colorIndex)) / 255.0);
			else if (colorIndex >= 224) // indices in [224, 239] are smoke
				COLOR = vec4(0.0, 0.0, 0.0, 0.1 + 0.032 * float(colorIndex - 224));
			else {
				vec2 paletteCoords = vec2(float(colorIndex % 16), float(colorIndex / 16)) / 16.0;
				bool tintedByCiv = (colorIndex < 16) || ((colorIndex < 64) && (colorIndex % 2 == 0));
				if (tintedByCiv)
					COLOR = sampleCivTintedColor(paletteCoords);
				else
					COLOR = texture(palette, paletteCoords);
			}
		}
		";
		var tr = new Shader();
		tr.Code = shaderSource;

		unitShader = tr;
		return tr;
	}

	private static Shader cursorShader = null;

	public static Shader getCursorShader()
	{
		if (cursorShader != null)
			return cursorShader;

		string shaderSource = @"
		shader_type canvas_item;

		uniform sampler2D palette;
		uniform sampler2D indices;
		uniform vec2 relSpriteSize; // sprite size relative to the entire sheet
		uniform vec2 spriteXY; // coordinates of the sprite to be drawn, in number of sprites not pixels

		void vertex()
		{
			UV = (spriteXY + UV) * relSpriteSize;
		}

		void fragment()
		{
			int colorIndex = int(255.0 * texture(indices, UV).r);
			if ((colorIndex >= 224) && (colorIndex <= 239))
				COLOR = vec4(1.0, 1.0, 1.0, float(colorIndex - 224) / float(239 - 224));
			else if (colorIndex >= 254) // indices 254 and 255 are transparent
				discard;
			else {
				vec2 paletteCoords = vec2(float(colorIndex % 16), float(colorIndex / 16)) / 16.0;
				COLOR = texture(palette, paletteCoords);
			}
		}
		";
		var tr = new Shader();
		tr.Code = shaderSource;

		cursorShader = tr;
		return tr;
	}
}

public class BuildingLayer : LooseLayer {
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
			var texRect = new Rect2(buildingSpriteSize * new Vector2 (2, 0), buildingSpriteSize);	//(2, 0) is the offset in the TerrainBuildings.PCX file (top row, third in)
			// TODO: Modify this calculation so it doesn't assume buildingSpriteSize is the same as the size of the terrain tiles
			var screenRect = new Rect2(tileCenter - (float)0.5 * buildingSpriteSize, buildingSpriteSize);
			looseView.DrawTextureRectRegion(buildingsTex, screenRect, texRect);
		}
	}
}

public class CityLayer : LooseLayer {

	private ILogger log = LogManager.ForContext<Game>();

	private ImageTexture cityTexture;
	private Vector2 citySpriteSize;
	private DynamicFont smallFont = new DynamicFont();
	private DynamicFont midSizedFont = new DynamicFont();
	private Pcx cityIcons = Util.LoadPCX("Art/Cities/city icons.pcx");
	private Image nonEmbassyStar;

	public CityLayer()
	{
		//TODO: Generalize, support multiple city types, etc.
		cityTexture = Util.LoadTextureFromPCX("Art/Cities/rROMAN.PCX", 0, 0, 167, 95);
		citySpriteSize = new Vector2(167, 95);
		smallFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
		smallFont.Size = 11;

		midSizedFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
		midSizedFont.Size = 18;

		nonEmbassyStar = PCXToGodot.getImageFromPCX(cityIcons, 20, 1, 18, 18);
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		if (tile.cityAtTile is null) {
			return;
		}

		City city = tile.cityAtTile;
		Rect2 screenRect = new Rect2(tileCenter - (float)0.5 * citySpriteSize, citySpriteSize);
		Rect2 textRect = new Rect2(new Vector2(0, 0), citySpriteSize);
		looseView.DrawTextureRectRegion(cityTexture, screenRect, textRect);

		int turnsUntilGrowth = city.TurnsUntilGrowth();
		string turnsUntilGrowthText = turnsUntilGrowth == int.MaxValue ? "- -" : "" + turnsUntilGrowth;
		String cityNameAndGrowth = $"{city.name} : {turnsUntilGrowthText}";
		String productionDescription = city.itemBeingProduced.name + " : " + city.TurnsUntilProductionFinished();

		int cityNameAndGrowthWidth = (int)smallFont.GetStringSize(cityNameAndGrowth).x;
		int productionDescriptionWidth = (int)smallFont.GetStringSize(productionDescription).x;
		int maxTextWidth = Math.Max(cityNameAndGrowthWidth, productionDescriptionWidth);

		int cityLabelWidth = maxTextWidth + (city.IsCapital()? 70 : 45);	//TODO: Is 65 right?  70?  Will depend on whether it's capital, too
		int textAreaWidth = cityLabelWidth - (city.IsCapital() ? 50 : 25);
		if (log.IsEnabled(LogEventLevel.Verbose)) {
			log.Verbose("Width of city name = " + maxTextWidth);
			log.Verbose("City label width: " + cityLabelWidth);
			log.Verbose("Text area width: " + textAreaWidth);
		}
		const int CITY_LABEL_HEIGHT = 23;
		const int TEXT_ROW_HEIGHT = 9;
		const int LEFT_RIGHT_BOXES_WIDTH = 24;
		const int LEFT_RIGHT_BOXES_HEIGHT = CITY_LABEL_HEIGHT - 2;

		//Label/name/producing area
		Image labelImage = new Image();
		labelImage.Create(cityLabelWidth, CITY_LABEL_HEIGHT, false, Image.Format.Rgba8);
		labelImage.Fill(Color.Color8(0, 0, 0, 0));
		byte transparencyLevel = 192;	//25%
		Color civColor = new Color(city.owner.color);
		civColor = new Color(civColor, transparencyLevel);
		Color civColorDarker = Color.Color8(0, 0, 138, transparencyLevel);	//todo: automate the darker() function.  maybe less transparency?
		Color topRowGrey = Color.Color8(32, 32, 32, transparencyLevel);
		Color bottomRowGrey = Color.Color8(48, 48, 48, transparencyLevel);
		Color backgroundGrey = Color.Color8(64, 64, 64, transparencyLevel);
		Color borderGrey = Color.Color8(80, 80, 80, transparencyLevel);

		Image horizontalBorder = new Image();
		horizontalBorder.Create(cityLabelWidth - 2, 1, false, Image.Format.Rgba8);
		horizontalBorder.Fill(borderGrey);
		labelImage.BlitRect(horizontalBorder, new Rect2(0, 0, new Vector2(cityLabelWidth - 2, 1)), new Vector2(1, 0));
		labelImage.BlitRect(horizontalBorder, new Rect2(0, 0, new Vector2(cityLabelWidth - 2, 1)), new Vector2(1, 22));

		Image verticalBorder = new Image();
		verticalBorder.Create(1, CITY_LABEL_HEIGHT - 2, false, Image.Format.Rgba8);
		verticalBorder.Fill(borderGrey);
		labelImage.BlitRect(verticalBorder, new Rect2(0, 0, new Vector2(1, 23)), new Vector2(0, 1));
		labelImage.BlitRect(verticalBorder, new Rect2(0, 0, new Vector2(1, 23)), new Vector2(cityLabelWidth - 1, 1));

		Image bottomRow = new Image();
		bottomRow.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
		bottomRow.Fill(bottomRowGrey);
		labelImage.BlitRect(bottomRow, new Rect2(0, 0, new Vector2(textAreaWidth, 1)), new Vector2(25, 21));

		Image topRow = new Image();
		topRow.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
		topRow.Fill(topRowGrey);
		labelImage.BlitRect(topRow, new Rect2(0, 0, new Vector2(textAreaWidth, 1)), new Vector2(25, 1));

		Image background = new Image();
		background.Create(textAreaWidth, TEXT_ROW_HEIGHT, false, Image.Format.Rgba8);
		background.Fill(backgroundGrey);
		labelImage.BlitRect(background, new Rect2(0, 0, new Vector2(textAreaWidth, 9)), new Vector2(25, 2));
		labelImage.BlitRect(background, new Rect2(0, 0, new Vector2(textAreaWidth, 9)), new Vector2(25, 12));

		Image centerDivider = new Image();
		centerDivider.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
		centerDivider.Fill(civColor);
		labelImage.BlitRect(centerDivider, new Rect2(0, 0, new Vector2(textAreaWidth, 1)), new Vector2(25, 11));

		Image leftAndRightBoxes = new Image();
		leftAndRightBoxes.Create(LEFT_RIGHT_BOXES_WIDTH, LEFT_RIGHT_BOXES_HEIGHT, false, Image.Format.Rgba8);
		leftAndRightBoxes.Fill(civColor);
		labelImage.BlitRect(leftAndRightBoxes, new Rect2(0, 0, new Vector2(24, 21)), new Vector2(1, 1));
		if (city.IsCapital()) {
			labelImage.BlitRect(leftAndRightBoxes, new Rect2(0, 0, new Vector2(24, 21)), new Vector2(cityLabelWidth - 25, 1));
			labelImage.BlendRect(nonEmbassyStar, new Rect2(0, 0, new Vector2(18, 18)), new Vector2(cityLabelWidth - 24, 2));
		}

		//todo: darker shades of civ color around edges

		ImageTexture label = new ImageTexture();
		label.CreateFromImage(labelImage, 0);

		Rect2 labelDestination = new Rect2(tileCenter + new Vector2(cityLabelWidth/-2, 24), new Vector2(cityLabelWidth, CITY_LABEL_HEIGHT));	//24 is a swag
		Rect2 allOfTheLabel = new Rect2(new Vector2(0, 0), new Vector2(cityLabelWidth, CITY_LABEL_HEIGHT));
		looseView.DrawTextureRectRegion(label, labelDestination, allOfTheLabel);

		//Destination for font is based on lower-left of baseline of font, not upper left as for blitted rectangles
		int cityNameOffset = cityNameAndGrowthWidth/-2;
		int prodDescriptionOffset = productionDescriptionWidth/-2;
		if (!city.IsCapital()) {
			cityNameOffset+=12;
			prodDescriptionOffset+=12;
		}
		Vector2 cityNameDestination = new Vector2(tileCenter + new Vector2(cityNameOffset, 24) + new Vector2(0, 10));
		looseView.DrawString(smallFont, cityNameDestination, cityNameAndGrowth, Color.Color8(255, 255, 255, 255));
		Vector2 productionDestination = new Vector2(tileCenter + new Vector2(prodDescriptionOffset, 24) + new Vector2(0, 20));
		looseView.DrawString(smallFont, productionDestination, productionDescription, Color.Color8(255, 255, 255, 255));

		//City pop size
		string popSizeString = "" + city.size;
		int popSizeWidth = (int)midSizedFont.GetStringSize(popSizeString).x;
		int popSizeOffset = LEFT_RIGHT_BOXES_WIDTH/2 - popSizeWidth/2;
		Vector2 popSizeDestination = new Vector2(tileCenter + new Vector2(cityLabelWidth/-2, 24) + new Vector2(popSizeOffset, 18));
		looseView.DrawString(midSizedFont, popSizeDestination, popSizeString, Color.Color8(255, 255, 255, 255));
	}
}

public class LooseView : Node2D {
	public MapView mapView;
	public List<LooseLayer> layers = new List<LooseLayer>();

	public LooseView(MapView mapView)
	{
		this.mapView = mapView;
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
			for (int y = visRegion.upperLeftY; y < visRegion.lowerRightY; y++)
				if (gD.map.isRowAt(y))
					for (int x = visRegion.getRowStartX(y); x < visRegion.lowerRightX; x += 2) {
						Tile tile = gD.map.tileAt(x, y);
						if (tile != Tile.NONE && gameDataAccess.gameData.GetHumanPlayers()[0].tileKnowledge.isTileKnown(tile))
							visibleTiles.Add(new VisibleTile { tile = tile, tileCenter = MapView.cellSize * new Vector2(x + 1, y + 1) });
					}

			foreach (LooseLayer layer in layers.FindAll(L => L.visible)) {
				layer.onBeginDraw(this, gD);
				foreach (VisibleTile vT in visibleTiles)
					layer.drawObject(this, gD, vT.tile, vT.tileCenter);
				layer.onEndDraw(this, gD);
			}


		}
	}
}

public class MapView : Node2D {
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
		looseView.layers.Add(new RoadLayer());
		looseView.layers.Add(new ResourceLayer());
		gridLayer = new GridLayer();
		looseView.layers.Add(gridLayer);
		looseView.layers.Add(new BuildingLayer());
		looseView.layers.Add(new UnitLayer());
		looseView.layers.Add(new CityLayer());

		(civColorWhitePalette, _) = Util.loadPalettizedPCX("Art/Units/Palettes/ntp00.pcx");

		AddChild(looseView);
	}

	public override void _Process(float delta)
	{
		// Redraw everything. This is necessary so that animations play. Maybe we could only update the unit layer but long term I think it's
		// better to redraw everything every frame like a typical modern video game.
		looseView.Update();
	}

	// Returns the size in pixels of the area in which the map will be drawn. This is the viewport size or, if that's null, the window size.
	public Vector2 getVisibleAreaSize()
	{
		var viewport = GetViewport();
		return (viewport != null) ? viewport.Size : OS.WindowSize;
	}

	public VisibleRegion getVisibleRegion()
	{
		(int x0, int y0) = tileCoordsOnScreenAt(new Vector2(0, 0));
		Vector2 mapViewSize = new Vector2(2, 4) + getVisibleAreaSize() / scaledCellSize;
		return new VisibleRegion { upperLeftX = x0 - 2, upperLeftY = y0 - 2,
			lowerRightX = x0 + (int)mapViewSize.x, lowerRightY = y0 + (int)mapViewSize.y };
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
		Vector2 mapPixelSize = new Vector2(cameraZoom, cameraZoom) * (new Vector2(cellSize.x * (mapWidth + 1), cellSize.y * (mapHeight + 1)));
		if (!wrapHorizontally) {
			float leftLim, rightLim;
			{
				if (mapPixelSize.x >= visAreaSize.x) {
					leftLim = 0;
					rightLim = mapPixelSize.x - visAreaSize.x;
				} else {
					leftLim = mapPixelSize.x - visAreaSize.x;
					rightLim = 0;
				}
			}
			if (location.x < leftLim)
				location.x = leftLim;
			else if (location.x > rightLim)
				location.x = rightLim;
		}
		if (!wrapVertically) {
			// These margins allow the player to move the camera that far off those map edges so that the UI controls don't cover up the
			// map. TODO: These values should be read from the sizes of the UI elements instead of hardcoded.
			float topMargin = 70, bottomMargin = 140;
			float topLim, bottomLim;
			{
				if (mapPixelSize.y >= visAreaSize.y) {
					topLim = -topMargin;
					bottomLim = mapPixelSize.y - visAreaSize.y + bottomMargin;
				} else {
					topLim = mapPixelSize.y - visAreaSize.y;
					bottomLim = 0;
				}
			}
			if (location.y < topLim)
				location.y = topLim;
			else if (location.y > bottomLim)
				location.y = bottomLim;
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
		int x = (int)intMapLoc.x, y = (int)intMapLoc.y;
		bool evenColumn = x%2 == 0, evenRow = y%2 == 0;
		if (evenColumn ^ evenRow) {
			if (fracMapLoc.y > fracMapLoc.x)
				x -= 1;
			else
				y -= 1;
		} else {
			if (fracMapLoc.y < 1 - fracMapLoc.x) {
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
