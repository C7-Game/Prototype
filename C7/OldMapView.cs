using System.Collections.Generic;
using C7.Map;
using Godot;
using ConvertCiv3Media;
using C7GameData;
using C7Engine;

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

public partial class GridLayer : LooseLayer {
	public Color color = Color.Color8(50, 50, 50, 150);
	public float lineWidth = (float)1.0;

	public GridLayer() {}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
	{
		Vector2 cS = OldMapView.cellSize;
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
	public OldMapView mapView;
	public List<LooseLayer> layers = new List<LooseLayer>();

	public LooseView(OldMapView mapView)
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
			OldMapView.VisibleRegion visRegion = mapView.getVisibleRegion();
			List<VisibleTile> visibleTiles = new List<VisibleTile>();
			for (int y = visRegion.upperLeftY; y < visRegion.lowerRightY; y++) {
				if (gD.map.isRowAt(y)) {
					for (int x = visRegion.getRowStartX(y); x < visRegion.lowerRightX; x += 2) {
						Tile tile = gD.map.tileAt(x, y);
						if (IsTileKnown(tile, gameDataAccess)) {
							visibleTiles.Add(new VisibleTile { tile = tile, tileCenter = OldMapView.cellSize * new Vector2(x + 1, y + 1) });
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
									VisibleTile invisibleTile = new VisibleTile { tile = tile, tileCenter = OldMapView.cellSize * new Vector2(x + 1, y + 1) };
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
	public OldMapView(Game game, int mapWidth, int mapHeight, bool wrapHorizontally, bool wrapVertically)
	{
		this.game = game;
		this.mapWidth = mapWidth;
		this.mapHeight = mapHeight;
		this.wrapHorizontally = wrapHorizontally;
		this.wrapVertically = wrapVertically;

		looseView = new LooseView(this);
		this.gridLayer = new GridLayer();
		looseView.layers.Add(this.gridLayer);
		looseView.layers.Add(new BuildingLayer());
		looseView.layers.Add(new CityLayer());

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
		Vector2 v2NewZoom = new Vector2(newScale, newScale);
		Vector2 v2OldZoom = new Vector2(cameraZoom, cameraZoom);
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
