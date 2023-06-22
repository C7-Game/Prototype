using C7.Map;
using Godot;
using C7GameData;

public partial class GridLayer {
	public Color color = Color.Color8(50, 50, 50, 150);
	public float lineWidth = (float)1.0;

	public GridLayer() {}

	public void drawObject(Tile tile, Vector2 tileCenter)
	{
		Vector2 cS = OldMapView.cellSize;
		Vector2 left  = tileCenter + new Vector2(-cS.X, 0    );
		Vector2 top   = tileCenter + new Vector2( 0   , -cS.Y);
		Vector2 right = tileCenter + new Vector2( cS.X, 0    );
		// DrawLine(left, top  , color, lineWidth);
		// DrawLine(top , right, color, lineWidth);
	}
}

public partial class OldMapView : Node2D {
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

	public struct VisibleRegion {
		public int upperLeftX, upperLeftY;
		public int lowerRightX, lowerRightY;

		public int getRowStartX(int y)
		{
			return upperLeftX + (y - upperLeftY)%2;
		}
	}

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
		Vector2 v2NewZoom = new Vector2(newScale, newScale);
		Vector2 v2OldZoom = new Vector2(cameraZoom, cameraZoom);
		if (v2NewZoom != v2OldZoom) {
			internalCameraZoom = newScale;
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
