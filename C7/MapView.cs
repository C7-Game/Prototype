using System.Collections.Generic;
using Godot;
using ConvertCiv3Media;
using C7GameData;
using C7Engine;

// Loose layers are for drawing free-form objects on the map. Unlike TileLayers (TODO: pull that out from MapView) they are not restricted to drawing
// tile sprites from a TileSet at fixed locations. They can draw anything, anywhere, although drawing is still tied to tiles for visibility
// purposes. The MapView contains a list of loose layers, each one an object that implements ILooseLayer. Right now to add a new layer you must modify
// the MapView constructor to add it to the list, but (TODO) eventually that will be made moddable.
public abstract class LooseLayer {
	// drawObject draws the things this layer is supposed to draw that are associated with the given tile. Its parameters are:
	//   looseView: The Node2D to actually draw to, e.g., use looseView.DrawCircle(...) to draw a circle. This object also contains a reference to
	//     the MapView in case you need it.
	//   tile: The game tile whose contents are to be drawn. This function gets called for each tile in view of the camera and none out of
	//     view. The same tile may be drawn multiple times at different locations due to edge wrapping.
	//   tileCenter: The location to draw to. You should draw around this location without adjusting for the camera location or zoom since the
	//     MapView already transforms the looseView node to account for those things.
	public abstract void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter);

	// The layer will be skipping during map drawing if visible is false
	public bool visible = true;
}

public class TerrainLayer : LooseLayer {

	public static readonly Vector2 terrainSpriteSize = new Vector2(128, 64);

	// A triple sheet is a sprite sheet containing sprites for three different terrain types including transitions between.
	private List<ImageTexture> tripleSheets;

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

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		int xSheet = tile.ExtraInfo.BaseTerrainImageID % 9, ySheet = tile.ExtraInfo.BaseTerrainImageID / 9;
		var texRect = new Rect2(new Vector2(xSheet, ySheet) * terrainSpriteSize, terrainSpriteSize);;
		var terrainOffset = new Vector2(0, -1 * MapView.cellSize.y);
		var screenRect = new Rect2(tileCenter - (float)0.5 * terrainSpriteSize + terrainOffset, terrainSpriteSize);
		looseView.DrawTextureRectRegion(tripleSheets[tile.ExtraInfo.BaseTerrainFileID], screenRect, texRect);
	}
}

public class GridLayer : LooseLayer {
	public Color color = Color.Color8(50, 50, 50, 150);
	public float lineWidth = (float)1.0;

	public GridLayer() {}

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
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

	public UnitLayer()
	{
		var iconPCX = new Pcx(Util.Civ3MediaPath("Art/Units/units_32.pcx"));
		unitIcons = PCXToGodot.getImageTextureFromPCX(iconPCX);
		unitIconsWidth = (unitIcons.GetWidth() - 1) / 33;

		var moveIndPCX = new Pcx(Util.Civ3MediaPath("Art/interface/MovementLED.pcx"));
		unitMovementIndicators = PCXToGodot.getImageTextureFromPCX(moveIndPCX);
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

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		if (tile.unitsOnTile.Count == 0)
			return;

		var white = Color.Color8(255, 255, 255);

		// Find unit to draw. If the currently selected unit is on this tile, use that one (also draw a yellow circle behind
		// it). Otherwise, use the top defender.
		MapUnit selectedUnitOnTile = null;
		foreach (var u in tile.unitsOnTile)
			if (u.guid == looseView.mapView.game.CurrentlySelectedUnit.guid) {
				looseView.DrawCircle(tileCenter, 16, Color.Color8(255, 255, 0));
				selectedUnitOnTile = u;
			}
		var unit = (selectedUnitOnTile != null) ? selectedUnitOnTile : tile.findTopDefender();

		// Draw colored circle at unit's feet to show who owns it
		looseView.DrawCircle(tileCenter, 8, new Color(unit.owner.color));

		int iconIndex = unit.unitType.iconIndex;
		Vector2 iconUpperLeft = new Vector2(1 + 33 * (iconIndex % unitIconsWidth), 1 + 33 * (iconIndex / unitIconsWidth));
		Rect2 unitRect = new Rect2(iconUpperLeft, new Vector2(32, 32));
		Rect2 screenRect = new Rect2(tileCenter - new Vector2(24, 40), new Vector2(48, 48));
		looseView.DrawTextureRectRegion(unitIcons, screenRect, unitRect);

		Vector2 indicatorLoc = tileCenter - new Vector2(26, 40);

		int mp = unit.movementPointsRemaining;
		int moveIndIndex = (mp <= 0) ? 4 : ((mp >= unit.unitType.movement) ? 0 : 2);
		Vector2 moveIndUpperLeft = new Vector2(1 + 7 * moveIndIndex, 1);
		Rect2 moveIndRect = new Rect2(moveIndUpperLeft, new Vector2(6, 6));
		screenRect = new Rect2(indicatorLoc, new Vector2(6, 6));
		looseView.DrawTextureRectRegion(unitMovementIndicators, screenRect, moveIndRect);

		int hpIndHeight = 20, hpIndWidth = 6;
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
}

public class BuildingLayer : LooseLayer {
	private ImageTexture buildingsTex;
	private Vector2 buildingSpriteSize;

	public BuildingLayer()
	{
		var buildingsPCX = new Pcx(Util.Civ3MediaPath("Art/Terrain/TerrainBuildings.PCX"));
		buildingsTex = PCXToGodot.getImageTextureFromPCX(buildingsPCX);
		buildingSpriteSize = new Vector2((float)buildingsTex.GetWidth() / 3, (float)buildingsTex.GetHeight() / 4);
	}

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		if (tile.hasBarbarianCamp) {
			var texRect = new Rect2(buildingSpriteSize * new Vector2 (2, 0), buildingSpriteSize);
			// TODO: Modify this calculation so it doesn't assume buildingSpriteSize is the same as the size of the terrain tiles
			var screenRect = new Rect2(tileCenter - (float)0.5 * buildingSpriteSize, buildingSpriteSize);
			looseView.DrawTextureRectRegion(buildingsTex, screenRect, texRect);
		}
	}
}

public class LooseView : Node2D {
	public MapView mapView;
	public List<LooseLayer> layers = new List<LooseLayer>();

	public LooseView(MapView mapView)
	{
		this.mapView = mapView;
	}

	public override void _Draw()
	{
		base._Draw();

		var map = MapInteractions.GetWholeMap();
		foreach (var layer in layers.FindAll(L => L.visible))
			foreach (var vT in mapView.visibleTiles()) {
				int x = mapView.wrapTileX(vT.virtTileX);
				int y = mapView.wrapTileY(vT.virtTileY);
				var tile = map.tileAt(x, y);
				Vector2 tileCenter = MapView.cellSize * new Vector2(x + 1, y + 1);
				layer.drawObject(this, tile, tileCenter);
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

	public struct VisibleTile {
		public int virtTileX, virtTileY; // (x, y) coords of the tile. These are "virtual", i.e. unwrapped, coordinates.
	}

	private LooseView looseView;

	public GridLayer gridLayer { get; private set; }

	public MapView(Game game, int mapWidth, int mapHeight, bool wrapHorizontally, bool wrapVertically)
	{
		this.game = game;
		this.mapWidth = mapWidth;
		this.mapHeight = mapHeight;
		this.wrapHorizontally = wrapHorizontally;
		this.wrapVertically = wrapVertically;

		looseView = new LooseView(this);
		looseView.layers.Add(new TerrainLayer());
		gridLayer = new GridLayer();
		looseView.layers.Add(gridLayer);
		looseView.layers.Add(new BuildingLayer());
		looseView.layers.Add(new UnitLayer());

		AddChild(looseView);

		onVisibleAreaChanged();
	}

	public bool isRowAt(int y)
	{
		return wrapVertically || ((y >= 0) && (y < mapHeight));
	}

	// TODO: Use function from GameMap (in C7GameData). Also copy isRowAt over there.
	public bool isTileAt(int x, int y)
	{
		bool evenRow = y%2 == 0;
		bool xInBounds; {
			if (wrapHorizontally)
				xInBounds = true;
			else if (evenRow)
				xInBounds = (x >= 0) && (x <= mapWidth - 2);
			else
				xInBounds = (x >= 1) && (x <= mapWidth - 1);
		}
		return isRowAt(y) && xInBounds && (evenRow ? (x%2 == 0) : (x%2 != 0));
	}

	public int wrapTileX(int x)
	{
		if (wrapHorizontally) {
			int tr = x % mapWidth;
			return (tr >= 0) ? tr : tr + mapWidth;
		} else
			return x;
	}

	public int wrapTileY(int y)
	{
		if (wrapVertically) {
			int tr = y % mapHeight;
			return (tr >= 0) ? tr : tr + mapHeight;
		} else
			return y;
	}

	// Returns the size in pixels of the area in which the map will be drawn. This is the viewport size or, if that's null, the window size.
	public Vector2 getVisibleAreaSize()
	{
		var viewport = GetViewport();
		return (viewport != null) ? viewport.Size : OS.WindowSize;
	}

	public IEnumerable<VisibleTile> visibleTiles()
	{
		int upperLeftX, upperLeftY;
		tileCoordsOnScreenAt(new Vector2(0, 0), out upperLeftX, out upperLeftY);
		Vector2 mapViewSize = new Vector2(2, 4) + getVisibleAreaSize() / scaledCellSize;
		for (int dy = -2; dy < mapViewSize.y; dy++) {
			int y = upperLeftY + dy;
			if (isRowAt(y))
				for (int dx = -2 + dy%2; dx < mapViewSize.x; dx += 2) {
					int x = upperLeftX + dx;
					if (isTileAt(x, y)) {
						VisibleTile tileInView = new VisibleTile();
						tileInView.virtTileX = x;
						tileInView.virtTileY = y;
						yield return tileInView;
					}
				}
		}
	}

	public void onVisibleAreaChanged()
	{
		// TODO: Update this comment and move it somewhere more appropriate
		// MapView is not the entire game map, rather it is a window into the game map that stays near the origin and covers the entire
		// screen. For small movements, the MapView itself is moved (amount is in cameraResidueX/Y) but once the movement equals an entire
		// grid cell (2 times the tile width or height) the map is snapped back toward the origin by that amount and to compensate it changes
		// what tiles are drawn (cameraTileX/Y). The advantage to doing things this way is that it makes it easy to duplicate tiles around
		// wrapped edges.

		looseView.Position = -cameraLocation;
		looseView.Update(); // trigger redraw
	}

	// "center" is the screen location around which the zoom is centered, e.g., if center is (0, 0) the tile in the top left corner will be the
	// same after the zoom level is changed, and if center is screenSize/2, the tile in the center of the window won't change.
	// This function does not adjust the zoom slider, so to keep the slider in sync with the actual zoom level, use AdjustZoomSlider. This
	// function must be separate, though, so that we can change the zoom level inside that callback without entering an infinite loop.
	public void setCameraZoom(float newScale, Vector2 center)
	{
		var v2NewZoom = new Vector2(newScale, newScale);
		var v2OldZoom = new Vector2(cameraZoom, cameraZoom);
		if (v2NewZoom != v2OldZoom) {
			internalCameraZoom = newScale;
			looseView.Scale = v2NewZoom;
			setCameraLocation ((v2NewZoom / v2OldZoom) * (cameraLocation + center) - center);
			// resetVisibleTiles(); // Don't have to call this because it's already called when the camera location is changed
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
		// TODO: Not quite perfect. When you zoom out you can still move the map a bit off the right/bottom edges.
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
		onVisibleAreaChanged();
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

	public void tileCoordsOnScreenAt(Vector2 screenLocation, out int x, out int y)
	{
		Vector2 mapLoc = (screenLocation + cameraLocation) / scaledCellSize;
		Vector2 intMapLoc = mapLoc.Floor();
		Vector2 fracMapLoc = mapLoc - intMapLoc;
		x = (int)intMapLoc.x;
		y = (int)intMapLoc.y;
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
	}

	// Returns the coordinates of the tile at the given screen location and true if there is one, otherwise returns (-1, -1) and false.
	public Tile tileOnScreenAt(Vector2 screenLocation)
	{
		int x, y;
		tileCoordsOnScreenAt(screenLocation, out x, out y);
		return MapInteractions.GetWholeMap().tileAt(wrapTileX(x), wrapTileY(y));
	}

	public void centerCameraOnTile(Tile t)
	{
		var tileCenter = new Vector2(t.xCoordinate + 1, t.yCoordinate + 1) * scaledCellSize;
		setCameraLocation(tileCenter - (float)0.5 * getVisibleAreaSize());
	}
}
