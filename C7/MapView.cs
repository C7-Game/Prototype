using System.Collections;
using Godot;
using ConvertCiv3Media;

public class MapView : Node2D
{
	public static readonly Vector2 tileSize = new Vector2(64, 32);

	// TODO: Getters and setters (where appropriate) for these.
	private int[,] terrain;
	private int mapWidth, mapHeight;
	private bool wrapHorizontally, wrapVertically;
	private Vector2 cameraLocation = new Vector2(0, 0);

	private TileMap terrainView;
	private TileSet terrainSet;

	public MapView(int[,] terrain, TileSet terrainSet, bool wrapHorizontally, bool wrapVertically)
	{
		this.terrain = terrain;
		this.terrainSet = terrainSet;
		mapWidth = terrain.GetLength(0);
		mapHeight = terrain.GetLength(1);
		this.wrapHorizontally = wrapHorizontally;
		this.wrapVertically = wrapVertically;

		initTerrainLayer();
	}

	public void initTerrainLayer() {
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		terrainView = new TileMap();
		terrainView.CellSize = tileSize;
		// terrainView.CenteredTextures = true;
		terrainView.TileSet = terrainSet;
		resetVisibleTiles();
		AddChild(terrainView);
	}

	public bool isInBounds(int x, int y)
	{
		bool xInBounds = wrapHorizontally || ((x >= 0) && (x < mapWidth));
		bool yInBounds = wrapVertically   || ((y >= 0) && (y < mapHeight));
		return xInBounds && yInBounds;
	}

	public int wrapTileX(int x)
	{
		int tr = x % mapWidth;
		return (tr >= 0) ? tr : tr + mapWidth;
	}

	public int wrapTileY(int y)
	{
		int tr = y % mapHeight;
		return (tr >= 0) ? tr : tr + mapHeight;
	}

	public void resetVisibleTiles()
	{
		terrainView.Clear();

		// MapView is not the entire game map, rather it is a window into the game map that stays near the origin and covers the entire
		// screen. For small movements, the MapView itself is moved (amount is in cameraResidueX/Y) but once the movement equals an entire
		// grid cell (2 times the tile width or height) the map is snapped back toward the origin by that amount and to compensate it changes
		// what tiles are drawn (cameraTileX/Y). The advantage to doing things this way is that it makes it easy to duplicate tiles around
		// wrapped edges.

		Vector2 tileFullSize = 2 * Scale * tileSize;

		int cameraPixelX = (int)cameraLocation.x;
		int fullTilesX = cameraPixelX / (int)tileFullSize.x;
		int cameraTileX = 2 * fullTilesX;
		int cameraResidueX = cameraPixelX - fullTilesX * (int)tileFullSize.x;

		int cameraPixelY = (int)cameraLocation.y;
		int fullTilesY = cameraPixelY / (int)tileFullSize.y;
		int cameraTileY = 2 * fullTilesY;
		int cameraResidueY = cameraPixelY - fullTilesY * (int)tileFullSize.y;

		GlobalPosition = new Vector2(-cameraResidueX, -cameraResidueY);

		// The offset of 4 is to ensure the bottom and right edges of the screen are covered
		int mapViewWidth  = 4 + (int)(OS.WindowSize.x / (Scale.x * tileSize.x));
		int mapViewHeight = 4 + (int)(OS.WindowSize.y / (Scale.y * tileSize.y));

		// loop to place tiles, each of which contains 1/4 of 4 'real' map locations
		// loops start at -3 and -6 to ensure the left and top (respectively) edges of the screen are covered
		for (int dy = -6; dy < mapViewHeight; dy++) {
			for (int dx = -3 - (dy%2); dx < mapViewWidth; dx+=2) {
				int x = cameraTileX + dx, y = cameraTileY + dy;
				if (isInBounds(x, y)) {
					terrainView.SetCell(dx, dy, terrain[wrapTileX(x), wrapTileY(y)]);
				}
			}
		}
	}

	// "center" is the screen location around which the zoom is centered, e.g., if center is (0, 0) the tile in the top left corner will be the
	// same after the zoom level is changed, and if center is screenSize/2, the tile in the center of the window won't change.
	// This function does not adjust the zoom slider, so to keep the slider in sync with the actual zoom level, use AdjustZoomSlider. This
	// function must be separate, though, so that we can change the zoom level inside that callback without entering an infinite loop.
	public void setCameraZoom(float newScale, Vector2 center)
	{
		Vector2 v2NewScale = new Vector2(newScale, newScale);
		Vector2 oldScale = Scale;
		if (v2NewScale != oldScale) {
			Scale = v2NewScale;
			setCameraLocation ((v2NewScale / oldScale) * (cameraLocation + center) - center);
			// RefillMapView(); // Don't have to call this because it's already called when the camera location is changed
		}
	}

	public void moveCamera(Vector2 offset)
	{
		setCameraLocation(cameraLocation + offset);
	}

	public void setCameraLocation(Vector2 location)
	{
		cameraLocation = location;

		// Prevent the camera from moving beyond an unwrapped edge of the map. One complication here is that the viewport might actually be
		// larger than the map (if we're zoomed far out) so in that case we must apply the constraint the other way around, i.e. constrain the
		// map to the viewport rather than the viewport to the map.
		// TODO: Not quite perfect. When you zoom out you can still move the map a bit off the right/bottom edges.
		Vector2 viewportSize = GetViewport().Size;
		Vector2 mapPixelSize = Scale * (new Vector2(tileSize.x * (mapWidth + 1), tileSize.y * (mapHeight + 1)));
		if (! wrapHorizontally) {
			float leftLim, rightLim; {
				if (mapPixelSize.x >= viewportSize.x) {
					leftLim = 0;
					rightLim = mapPixelSize.x - viewportSize.x;
				} else {
					leftLim = mapPixelSize.x - viewportSize.x;
					rightLim = 0;
				}
			}
			if (cameraLocation.x < leftLim)
				cameraLocation.x = leftLim;
			else if (cameraLocation.x > rightLim)
				cameraLocation.x = rightLim;
		}
		if (! wrapVertically) {
			// These margins allow the player to move the camera that far off those map edges so that the UI controls don't cover up the
			// map. TODO: These values should be read from the sizes of the UI elements instead of hardcoded.
			float topMargin = 70, bottomMargin = 140;
			float topLim, bottomLim; {
				if (mapPixelSize.y >= viewportSize.y) {
					topLim = -topMargin;
					bottomLim = mapPixelSize.y - viewportSize.y + bottomMargin;
				} else {
					topLim = mapPixelSize.y - viewportSize.y;
					bottomLim = 0;
				}
			}
			if (cameraLocation.y < topLim)
				cameraLocation.y = topLim;
			else if (cameraLocation.y > bottomLim)
				cameraLocation.y = bottomLim;
		}

		resetVisibleTiles();
	}


}
