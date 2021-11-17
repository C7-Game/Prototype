using System.Collections.Generic;
using Godot;
using ConvertCiv3Media;
using C7GameData;
using C7Engine;

// Loose layers are for drawing free-form objects on the map. Unlike TileLayers (TODO: pull that out from MapView) they are not restricted to drawing
// tile sprites from a TileSet at fixed locations. They can draw anything, anywhere, although drawing is still tied to tiles for visibility
// purposes. The MapView contains a list of loose layers, each one an object that implements ILooseLayer. Right now to add a new layer you must modify
// the MapView constructor to add it to the list, but (TODO) eventually that will be made moddable.
public interface ILooseLayer {
	// drawObject draws the things this layer is supposed to draw that are associated with the given tile. Its parameters are:
	//   looseView: The Node2D to actually draw to, e.g., use looseView.DrawCircle(...) to draw a circle. This object also contains a reference to
	//     the MapView in case you need it.
	//   tile: The game tile whose contents are to be drawn. This function gets called for each tile in view of the camera and none out of
	//     view. The same tile may be drawn multiple times at different locations due to edge wrapping.
	//   tileCenter: The location to draw to. You should draw around this location without adjusting for the camera location or zoom since the
	//     MapView already transforms the looseView node to account for those things.
	void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter);
}

public class UnitLayer : ILooseLayer {
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

	public void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		if (tile.unitsOnTile.Count == 0)
			return;

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
				looseView.DrawRect(hpIndBackgroundRect, Color.Color8(255, 255, 255), false);
		}
	}
}

public class LooseView : Node2D {
	public MapView mapView;
	public List<ILooseLayer> layers = new List<ILooseLayer>();

	public LooseView(MapView mapView)
	{
		this.mapView = mapView;
	}

	public override void _Draw()
	{
		base._Draw();

		var map = MapInteractions.GetWholeMap();
		foreach (var layer in layers)
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

	// Normally the camera location is stored in pixels in map coords, the cameraLocationInCells property gives the camera location in grid
	// cells. cellX/Y is the whole number of cells and residueX/Y is the pixel location inside the cell.
	public struct CameraLocationInCells {
		public int cellsX, cellsY;
		public int residueX, residueY;
	}
	public CameraLocationInCells cameraLocationInCells {
		get {
			var tr = new CameraLocationInCells();

			Vector2 tileSize = 2 * scaledCellSize;

			int cameraPixelX = (int)cameraLocation.x;
			int tilesX = cameraPixelX / (int)tileSize.x;
			tr.cellsX = 2 * tilesX;
			tr.residueX = cameraPixelX - tilesX * (int)tileSize.x;

			int cameraPixelY = (int)cameraLocation.y;
			int tilesY = cameraPixelY / (int)tileSize.y;
			tr.cellsY = 2 * tilesY;
			tr.residueY = cameraPixelY - tilesY * (int)tileSize.y;

			return tr;
		}
	}

	public struct VisibleTile {
		public int virtTileX, virtTileY; // (x, y) coords of the tile. These are "virtual", i.e. unwrapped, coordinates.
		public int viewX, viewY; // coordinates of the cell where the tile is be drawn, for use by tiled (not loose) layers
	}

	private int[,] terrain;
	private TileMap terrainView;
	private TileSet terrainSet;

	private LooseView looseView;

	public MapView(Game game, int[,] terrain, TileSet terrainSet, bool wrapHorizontally, bool wrapVertically)
	{
		this.game = game;
		this.terrain = terrain;
		this.terrainSet = terrainSet;
		mapWidth = terrain.GetLength(0);
		mapHeight = terrain.GetLength(1);
		this.wrapHorizontally = wrapHorizontally;
		this.wrapVertically = wrapVertically;

		looseView = new LooseView(this);

		// Initialize layers
		initTerrainLayer();
		looseView.layers.Add(new UnitLayer());

		AddChild(looseView);

		onVisibleAreaChanged();
	}

	public void initTerrainLayer()
	{
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		terrainView = new TileMap();
		terrainView.CellSize = cellSize;
		// terrainView.CenteredTextures = true;
		terrainView.TileSet = terrainSet;
		AddChild(terrainView);
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
	private Vector2 getVisibleAreaSize()
	{
		var viewport = GetViewport();
		return (viewport != null) ? viewport.Size : OS.WindowSize;
	}

	public IEnumerable<VisibleTile> visibleTiles()
	{
		var cLIC = cameraLocationInCells;
		Vector2 mapViewSize = new Vector2(2, 2) + getVisibleAreaSize() / scaledCellSize;
		for (int dy = -2; dy < mapViewSize.y; dy++) {
			int y = cLIC.cellsY + dy;
			if (isRowAt(y))
				for (int dx = -2 + dy%2; dx < mapViewSize.x; dx += 2) {
					int x = cLIC.cellsX + dx;
					if (isTileAt(x, y)) {
						VisibleTile tileInView = new VisibleTile();
						tileInView.virtTileX = x;
						tileInView.virtTileY = y;
						tileInView.viewX = dx;
						tileInView.viewY = dy;
						yield return tileInView;
					}
				}
		}
	}

	public void onVisibleAreaChanged()
	{
		terrainView.Clear();

		// TODO: Update this comment and move it somewhere more appropriate
		// MapView is not the entire game map, rather it is a window into the game map that stays near the origin and covers the entire
		// screen. For small movements, the MapView itself is moved (amount is in cameraResidueX/Y) but once the movement equals an entire
		// grid cell (2 times the tile width or height) the map is snapped back toward the origin by that amount and to compensate it changes
		// what tiles are drawn (cameraTileX/Y). The advantage to doing things this way is that it makes it easy to duplicate tiles around
		// wrapped edges.

		var cLIC = cameraLocationInCells;

		// Update layer positions
		terrainView.Position = new Vector2(-cLIC.residueX, -cLIC.residueY);
		looseView.Position = -cameraLocation;

		foreach (var vT in visibleTiles())
			terrainView.SetCell(vT.viewX, vT.viewY, terrain[wrapTileX(vT.virtTileX), wrapTileY(vT.virtTileY)]);

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
			terrainView.Scale = v2NewZoom;
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

	// Returns the location of tile (x, y) on the screen, if "center" is true returns the location of the tile center and otherwise returns the
	// upper left. Works even if (x, y) is off screen or out of bounds.
	public Vector2 screenLocationOfTile(int x, int y, bool center = true)
	{
		var cLIC = cameraLocationInCells;

		// Add one to x & y to get the tile center b/c in Civ 3 the tile at (x, y) is a diamond centered on (x+1, y+1).
		Vector2 centeringOffset = center ? new Vector2(1, 1) : new Vector2(0, 0);

		// cameraTileX/Y is what gets drawn at (0, 0) in MapView's local coordinates.
		return terrainView.Position + (centeringOffset + new Vector2(x - cLIC.cellsX, y - cLIC.cellsY)) * scaledCellSize;
	}

	// Returns the coordinates of the tile at the given screen location and true if there is one, otherwise returns (-1, -1) and false.
	public bool tileOnScreenAt(Vector2 screenLocation, out int tileX, out int tileY)
	{
		// TODO: This calculation could be made a lot more efficient by inlining screenLocationOfTile since right now it undoes several things
		// that function does. Though this way it's more clear how the algorithm works.
		Vector2 mapLoc = (screenLocation - screenLocationOfTile(0, 0, false)) / scaledCellSize;
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
		x = wrapTileX(x);
		y = wrapTileY(y);
		if (isTileAt(x, y)) {
			tileX = x;
			tileY = y;
			return true;
		} else {
			tileX = -1;
			tileY = -1;
			return false;
		}
	}

}
