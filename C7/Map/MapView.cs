using Godot;
using C7GameData;
using System;
using Serilog;
using System.Linq;
using System.Collections.Generic;
using C7Engine;

namespace C7.Map {

	public static class MapZIndex {
		public static readonly int Tiles = 10;
		public static readonly int Cities = 20;
		public static readonly int Cursor = 29;
		public static readonly int Units = 30;
		public static readonly int FogOfWar = 100;
	}

	public enum HorizontalWrapState {
		Left,  // beyond left edge of the map is visible
		Right, // beyond right edge of the map is visible
		None,  // camera is entirely over the map
	}

	public partial class MapView : Node2D {
		private string[,]terrain;
		private TileMap terrainTilemap;
		private TileMap wrappingTerrainTilemap;
		private TileSet terrainTileset;
		private TileMap tilemap;
		private TileMap wrappingTilemap;
		private TileSet tileset;
		private Vector2I tileSize = new Vector2I(128, 64);
		private ILogger log = LogManager.ForContext<MapView>();
		public bool wrapHorizontally {get; private set;}
		public int worldEdgeRight {get; private set;}
		public int worldEdgeLeft {get; private set;}
		private int width;
		private int height;
		private bool showGrid = false;
		private void setShowGrid(bool value) {
 			bool update = showGrid != value;
 			showGrid = value;
 			if (update) {
 				updateGridLayer();
 			}
 		}
 		public void toggleGrid() {
 			setShowGrid(!showGrid);
 		}
		public int pixelWidth {get; private set;}
		private Game game;
		private GameData data;
		private GameMap gameMap;

		private Dictionary<MapUnit, UnitSprite> unitSprites = new Dictionary<MapUnit, UnitSprite>();
		private Dictionary<Tile, CityScene> cityScenes = new Dictionary<Tile, CityScene>();
		private CursorSprite cursor;

		private UnitSprite spriteFor(MapUnit unit) {
			UnitSprite sprite = unitSprites.GetValueOrDefault(unit, null);
			if (sprite is null) {
				sprite = new UnitSprite(game.civ3AnimData);
				unitSprites.Add(unit, sprite);
				AddChild(sprite);
			}
			return sprite;
		}

		private Vector2 getSpriteLocalPosition(Tile tile, MapUnit.Appearance appearance) {
			Vector2 position = tilemap.MapToLocal(stackedCoords(tile));
			Vector2 offset = tileSize * new Vector2(appearance.offsetX, appearance.offsetY) / 2;
			return position + offset;
		}

		public void addCity(City city, Tile tile) {
			log.Debug($"adding city at tile ({tile.xCoordinate}, {tile.yCoordinate})");
			CityScene scene = new CityScene(city, tile);
			scene.Position = tilemap.MapToLocal(stackedCoords(tile));
			AddChild(scene);
			cityScenes.Add(tile, scene);
		}

		private Vector2I horizontalWrapOffset(HorizontalWrapState wrap) {
			return wrap switch {
				HorizontalWrapState.Left => Vector2I.Left,
				HorizontalWrapState.Right => Vector2I.Right,
				_ => Vector2I.Zero,
			} * pixelWidth;
		}

		private void animateUnit(Tile tile, MapUnit unit, HorizontalWrapState wrap) {
			// TODO: simplify AnimationManager and drawing animations it is unnecessarily complex
			// - also investigate if the custom offset tracking and SetFrame can be replaced by
			//   engine functionality
			MapUnit.Appearance appearance = game.animTracker.getUnitAppearance(unit);
			string name = AnimationManager.AnimationKey(unit.unitType, appearance.action, appearance.direction);
			C7Animation animation = game.civ3AnimData.forUnit(unit.unitType, appearance.action);
			animation.loadSpriteAnimation();
			UnitSprite sprite = spriteFor(unit);
			int frame = sprite.GetNextFrameByProgress(name, appearance.progress);
			float yOffset = sprite.FrameSize(name).Y / 4f; // TODO: verify actual value
			Vector2 position = getSpriteLocalPosition(tile, appearance);
			sprite.Position = position - new Vector2(0, yOffset);
			Color civColor = new Color(unit.owner.color);
			sprite.SetColor(civColor);
			sprite.SetAnimation(name);
			sprite.SetFrame(frame);
			sprite.Show();

			Vector2 wrapOffset = horizontalWrapOffset(wrap);
			sprite.Translate(wrapOffset);

			if (unit == game.CurrentlySelectedUnit) {
				// TODO: just noticed cursor position maybe should not be
				// on sprite position which has a potential offset?
				cursor.Position = position + wrapOffset;
				cursor.Show();
			}
		}

		private MapUnit selectUnitToDisplay(List<MapUnit> units) {
			if (units.Count == 0) {
				return MapUnit.NONE;
			}
			MapUnit bestDefender = units[0], selected = null, interesting = null;
			MapUnit currentlySelected = game.CurrentlySelectedUnit;
			foreach (MapUnit unit in units) {
				if (unit == currentlySelected) {
					selected = unit;
				}
				if (unit.HasPriorityAsDefender(bestDefender, currentlySelected)) {
					bestDefender = unit;
				}
				if (game.animTracker.getUnitAppearance(unit).DeservesPlayerAttention()) {
					interesting = unit;
				}
			}
			// Prefer showing the selected unit, secondly show one doing a relevant animation, otherwise show the top defender
			return selected ?? interesting ?? bestDefender;
		}

		public List<(Tile, HorizontalWrapState)> getVisibleTiles() {
			List<(Tile, HorizontalWrapState)> tiles = new List<(Tile, HorizontalWrapState)>();
			Rect2 bounds = game.camera.getVisibleWorld();
			Vector2I topLeft = tilemap.LocalToMap(ToLocal(bounds.Position));
			Vector2I bottomRight = tilemap.LocalToMap(ToLocal(bounds.End));
			for (int x = topLeft.X - 1; x < bottomRight.X + 1; x++) {
				for (int y = topLeft.Y - 1; y < bottomRight.Y + 1; y++) {
					(int usX, int usY) = unstackedCoords(new Vector2I(x, y));
					HorizontalWrapState wrap = x switch {
						_ when x < 0 => HorizontalWrapState.Left,
						_ when x >= width => HorizontalWrapState.Right,
						_ => HorizontalWrapState.None,
					};
					tiles.Add((data.map.tileAt(usX, usY), wrap));
				}
			}
			return tiles;
		}

		public void updateAnimations() {
			foreach (UnitSprite s in unitSprites.Values) {
				s.Hide();
			}
			cursor.Hide();
			foreach ((Tile tile, HorizontalWrapState wrap) in getVisibleTiles()) {
				MapUnit unit = selectUnitToDisplay(tile.unitsOnTile);
				if (unit != MapUnit.NONE) {
					animateUnit(tile, unit, wrap);
				}
				if (cityScenes.ContainsKey(tile)) {
					CityScene scene = cityScenes[tile];
					Vector2 position = tilemap.MapToLocal(stackedCoords(tile)) + horizontalWrapOffset(wrap);
					scene.Position = position;
				}
			}
		}

		public void setHorizontalWrap(HorizontalWrapState state) {
			if (state == HorizontalWrapState.None) {
				wrappingTerrainTilemap.Hide();
				wrappingTilemap.Hide();
			} else {
				Vector2I offset = horizontalWrapOffset(state);
				wrappingTerrainTilemap.Show();
				wrappingTilemap.Show();
				wrappingTerrainTilemap.Position = terrainTilemap.Position + offset;
				wrappingTilemap.Position = tilemap.Position + offset;
			}
		}

		private void initializeTileMap() {
			terrainTilemap = new TileMap();
			wrappingTerrainTilemap = new TileMap();
			terrainTileset = Civ3TerrainTileSet.Generate();
			terrainTilemap.TileSet = terrainTileset;
			terrainTilemap.Position += Vector2I.Right * (tileSize.X / 2);
			wrappingTerrainTilemap.TileSet = terrainTileset;

			tilemap = new TileMap{ YSortEnabled = true };
			wrappingTilemap = new TileMap { YSortEnabled = true };

			tileset = TileSetLoader.LoadCiv3TileSet();
			tilemap.TileSet = tileset;
			wrappingTilemap.TileSet = tileset;

			// create tilemap layers
			foreach (Layer layer in Enum.GetValues(typeof(Layer))) {
				if (layer != Layer.Invalid) {
					tilemap.AddLayer(layer.Index());
					wrappingTilemap.AddLayer(layer.Index());
					if (layer != Layer.FogOfWar) {
 						tilemap.SetLayerYSortEnabled(layer.Index(), true);
						wrappingTilemap.SetLayerYSortEnabled(layer.Index(), true);
 					} else {
 						tilemap.SetLayerZIndex(layer.Index(), MapZIndex.FogOfWar);
						wrappingTilemap.SetLayerZIndex(layer.Index(), MapZIndex.FogOfWar);
 					}
				}
			}

			setHorizontalWrap(HorizontalWrapState.None);

			tilemap.ZIndex = 10; // need to figure out a good way to order z indices
			wrappingTilemap.ZIndex = 10;
			AddChild(tilemap);
			AddChild(wrappingTilemap);
			AddChild(terrainTilemap);
			AddChild(wrappingTerrainTilemap);
		}

		private void setTerrainTiles() {
			string[] corners(int x, int y) {
				string left = terrain[x, y];
				string right = terrain[(x + 1) % width, y];
				bool even = y % 2 == 0;
				string top = "coast";
				if (y > 0) {
					top = even ? terrain[x, y - 1] : terrain[(x + 1) % width, y - 1];
				}
				string bottom = "coast";
				if (y < height - 1) {
					bottom = even ? terrain[x, y + 1] : terrain[(x + 1) % width, y + 1];
				}
				return new string[4]{top, right, bottom, left};
			}
			void lookupAndSetTerrainTile(int x, int y, int cellX, int cellY) {
				Vector2I cell = new Vector2I(cellX, cellY);
				string[] corner = corners(x, y);
				TerrainPcx pcx = Civ3TerrainTileSet.GetPcxFor(corner);
				Vector2I texCoords = pcx.getTextureCoords(corner);
				setTerrainTile(cell, pcx.atlas, texCoords);
			}
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					lookupAndSetTerrainTile(x, y, x, y);
				}
			}
			for (int y = 0; y < height; y++) {
				lookupAndSetTerrainTile(width - 1, y, -1, y);
			}
		}

		void setTerrainTile(Vector2I cell, int atlas, Vector2I texCoords) {
			terrainTilemap.SetCell(0, cell, atlas, texCoords);
			wrappingTerrainTilemap.SetCell(0, cell, atlas, texCoords);
		}

		private Vector2I stackedCoords(Tile tile) {
			int x = tile.xCoordinate;
			int y = tile.yCoordinate;
			x = y % 2 == 0 ? x / 2 : (x - 1) / 2;
			return new Vector2I(x, y);
		}

		private (int, int) unstackedCoords(Vector2I stacked) {
			(int x, int y) = (stacked.X, stacked.Y);
			x = y % 2 == 0 ? x * 2 : (x * 2) + 1;
			return (x, y);
		}

		public MapView(Game game, GameData data) {
			this.data = data;
			this.game = game;
			this.data = data;
			this.gameMap = data.map;
			cursor = new CursorSprite();
			AddChild(cursor);
			width = gameMap.numTilesWide / 2;
			pixelWidth = width * tileSize.X;
			height = gameMap.numTilesTall;
			initializeTileMap();
			wrapHorizontally = gameMap.wrapHorizontally;
			terrain = new string[width, height];
			worldEdgeRight = (int)ToGlobal(tilemap.MapToLocal(new Vector2I(width - 1, 1))).X + tileSize.X / 2;
			worldEdgeLeft = (int)ToGlobal(tilemap.MapToLocal(new Vector2I(0, 0))).X - tileSize.X / 2;

			// Convert coordinates from current save coordinates to
			// stacked coordinates used by Godot's TileMap, and
			// write terrain types to 2D array for generating corners
			// TODO in the future convert civ3 coordinates to stacked
			// coordinates when reading from the civ3 save so Tile has
			// stacked coordinates
			foreach (C7GameData.Tile t in gameMap.tiles) {
				Vector2I coords = stackedCoords(t);
				terrain[coords.X, coords.Y] = t.baseTerrainTypeKey;
			}
			setTerrainTiles();

			// update each tile once to add all initial layers
			TileKnowledge tk = data.GetHumanPlayers().First()?.tileKnowledge;
			foreach (Tile tile in gameMap.tiles) {
				updateTile(tile, tk);
			}
		}

		public Tile tileAt(GameMap gameMap, Vector2 globalMousePosition) {
			Vector2I tilemapCoord = tilemap.LocalToMap(ToLocal(globalMousePosition));
			(int x, int y) = unstackedCoords(tilemapCoord);
			return gameMap.tileAt(x, y);
		}

		public Vector2 tileToLocal(Tile tile) => tilemap.MapToLocal(stackedCoords(tile));

		private void setCell(Layer layer, Atlas atlas, Tile tile, Vector2I atlasCoords) {
			if (!tileset.HasSource(atlas.Index())) {
				log.Warning($"atlas id {atlas} is not a valid tileset source");
			}
			if (!tileset.GetSource(atlas.Index()).HasTile(atlasCoords)) {
				log.Warning($"atlas id {atlas} does not have tile at {atlasCoords}");
			}
			tilemap.SetCell(layer.Index(), stackedCoords(tile), atlas.Index(), atlasCoords);
			wrappingTilemap.SetCell(layer.Index(), stackedCoords(tile), atlas.Index(), atlasCoords);
		}

		private void eraseCell(Layer layer, Tile tile) {
			tilemap.EraseCell(layer.Index(), stackedCoords(tile));
			wrappingTilemap.EraseCell(layer.Index(), stackedCoords(tile));
		}

		private void updateRoadLayer(Tile tile, bool center) {
			if (!tile.overlays.road) {
				eraseCell(Layer.Road, tile);
				eraseCell(Layer.Rail, tile);
				return;
			}
			if (!tile.overlays.railroad) {
				// road
				int index = 0;
				foreach ((TileDirection direction, Tile neighbor) in tile.neighbors) {
					if (neighbor.overlays.road) {
						index |= roadFlag(direction);
					}
				}
				eraseCell(Layer.Rail, tile);
				setCell(Layer.Road, Atlas.Road, tile, roadIndexTo2D(index));
			} else {
				// railroad
				int roadIndex = 0;
				int railIndex = 0;
				foreach ((TileDirection direction, Tile neighbor) in tile.neighbors) {
					if (neighbor.overlays.railroad) {
						railIndex |= roadFlag(direction);
					} else if (neighbor.overlays.road) {
						roadIndex |= roadFlag(direction);
					}
				}
				if (roadIndex != 0) {
					setCell(Layer.Road, Atlas.Road, tile, roadIndexTo2D(roadIndex));
				} else {
					eraseCell(Layer.Road, tile);
				}
				setCell(Layer.Rail, Atlas.Rail, tile, roadIndexTo2D(railIndex));
			}

			if (center) {
				// updating a tile may change neighboring tiles
				foreach (Tile neighbor in tile.neighbors.Values) {
					updateRoadLayer(neighbor, false);
				}
			}
		}

		private Vector2I roadIndexTo2D(int index) => new Vector2I(index & 0xF, index >> 4);

		private static int roadFlag(TileDirection direction) {
			return direction switch {
				TileDirection.NORTHEAST => 0x1,
				TileDirection.EAST => 0x2,
				TileDirection.SOUTHEAST => 0x4,
				TileDirection.SOUTH => 0x8,
				TileDirection.SOUTHWEST => 0x10,
				TileDirection.WEST => 0x20,
				TileDirection.NORTHWEST => 0x40,
				TileDirection.NORTH => 0x80,
				_ => throw new ArgumentOutOfRangeException("Invalid TileDirection")
			};
		}

		private void updateRiverLayer(Tile tile) {
			// The "point" is the easternmost point of the current tile.
			// The river graphic is determined by the tiles neighboring that point.
			Tile northOfPoint = tile.neighbors[TileDirection.NORTHEAST];
			Tile eastOfPoint = tile.neighbors[TileDirection.EAST];
			Tile westOfPoint = tile;
			Tile southOfPoint = tile.neighbors[TileDirection.SOUTHEAST];

			int index = 0;
			index += northOfPoint.riverSouthwest ? 1 : 0;
			index += eastOfPoint.riverNorthwest ? 2 : 0;
			index += westOfPoint.riverSoutheast ? 4 : 0;
			index += southOfPoint.riverNortheast ? 8 : 0;

			if (index == 0) {
				eraseCell(Layer.River, tile);
			} else {
				setCell(Layer.River, Atlas.River, tile, new Vector2I(index % 4, index / 4));
			}
		}

		private void updateHillLayer(Tile tile) {
			if (!tile.overlayTerrainType.isHilly()) {
				return;
			}
			Vector2I texCoord = getHillTextureCoordinate(tile);
			TerrainType nearbyVegitation = getDominantVegetationNearHillyTile(tile);
			Atlas atlas = tile.overlayTerrainType.Key switch {
				"hills" => nearbyVegitation.Key switch {
					"forest" => Atlas.ForestHill,
					"jungle" => Atlas.JungleHill,
					_ => Atlas.Hill,
				},
				"mountains" => nearbyVegitation.Key switch {
					_ when tile.isSnowCapped => Atlas.SnowMountain,
					"forest" => Atlas.ForestMountain,
					"jungle" => Atlas.JungleMountain,
					_ => Atlas.Mountain,
				},
				"volcano" => nearbyVegitation.Key switch {
					"forest" => Atlas.ForestVolcano,
					"jungle" => Atlas.JungleVolcano,
					_ => Atlas.Volcano,
				},
				_ => Atlas.Invalid,
			};
			if (atlas != Atlas.Invalid) {
				setCell(Layer.TerrainOverlay, atlas, tile, texCoord);
			}
		}

		private Vector2I getHillTextureCoordinate(Tile tile) {
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
			return new Vector2I(index % 4, index / 4);
		}

		private TerrainType getDominantVegetationNearHillyTile(Tile center) {
			TerrainType northeastType = center.neighbors[TileDirection.NORTHEAST].overlayTerrainType;
			TerrainType northwestType = center.neighbors[TileDirection.NORTHWEST].overlayTerrainType;
			TerrainType southeastType = center.neighbors[TileDirection.SOUTHEAST].overlayTerrainType;
			TerrainType southwestType = center.neighbors[TileDirection.SOUTHWEST].overlayTerrainType;

			TerrainType[] neighborTerrains = { northeastType, northwestType, southeastType, southwestType };

			int hills = neighborTerrains.Where(tt => tt.isHilly()).Count();
			TerrainType forest = neighborTerrains.FirstOrDefault(tt => tt.Key == "forest", null);
			int forests = neighborTerrains.Where(tt => tt.Key == "forest").Count();
			TerrainType jungle = neighborTerrains.FirstOrDefault(tt => tt.Key == "jungle", null);
			int jungles = neighborTerrains.Where(tt => tt.Key == "jungle").Count();

			if (hills + forests + jungles < 4) { // some surrounding tiles are neither forested nor hilly
				return TerrainType.NONE;
			}
			if (forests == 0 && jungles == 0) {
				return TerrainType.NONE; // all hills
			}
			if (forests == jungles) {
				// deterministically choose one on a tie so it doesn't change if the tile is updated
				return center.xCoordinate % 2 == 0 ? forest : jungle;
			}
			return forests > jungles ? forest : jungle;
		}

		private void updateForestLayer(Tile tile) {
			if (tile.overlayTerrainType.Key == "forest") {
				(int row, int col) = (0, 0);
				if (tile.isPineForest) {
					row = 8 + tile.xCoordinate % 2; // pine starts at row 8 in atlas
					col = tile.xCoordinate % 6;     // pine has 6 columns
				} else {
					bool small = tile.numWaterEdges() > 0;
					// this technically omits one large and one small tile but the math is simpler
					if (small) {
						row = 6 + tile.xCoordinate % 2;
						col = 1 + tile.xCoordinate % 4;
					} else {
						row = 4 + tile.xCoordinate % 2;
						col = tile.xCoordinate % 4;
					}
				}
				Atlas atlas = tile.baseTerrainType.Key switch {
					"plains" => Atlas.PlainsForest,
					"grassland" => Atlas.GrasslandsForest,
					"tundra" => Atlas.TundraForest,
					_ => Atlas.PlainsForest,
				};
				setCell(Layer.TerrainOverlay, atlas, tile, new Vector2I(col, row));
			} else if (tile.overlayTerrainType.Key == "jungle") {
				// Randomly, but predictably, choose a large jungle graphic
				// More research is needed on when to use large vs small jungles. Probably, small is used when neighboring fewer jungles.
				// For the first pass, we're just always using large jungles.
				(int row, int col) = (tile.xCoordinate % 2, tile.xCoordinate % 4);
				if (tile.numWaterEdges() > 0) {
					(row, col) = (2 + tile.xCoordinate % 2, 1 + tile.xCoordinate % 5);
				}
				setCell(Layer.TerrainOverlay, Atlas.GrasslandsForest, tile, new Vector2I(col, row));
			}
		}

		private void updateMarshLayer(Tile tile) {
			if (tile.overlayTerrainType.Key != "marsh") {
				return;
			}
			(int row, int col) = (tile.xCoordinate % 2, tile.xCoordinate % 4);
			if (tile.numWaterEdges() > 0) {
				(row, col) = (2 + tile.xCoordinate % 2, 1 + tile.xCoordinate % 4);
			}
			setCell(Layer.TerrainOverlay, Atlas.Marsh, tile, new Vector2I(col, row));
		}

		private static bool isForest(Tile tile) {
			return tile.overlayTerrainType.Key == "forest" || tile.overlayTerrainType.Key == "jungle";
		}

		private void updateTerrainOverlayLayer(Tile tile) {
			if (!tile.overlayTerrainType.isHilly() && !isForest(tile) && tile.overlayTerrainType.Key != "marsh") {
				eraseCell(Layer.TerrainOverlay, tile);
				return;
			}
			if (tile.overlayTerrainType.isHilly()) {
				updateHillLayer(tile);
			} else if (isForest(tile)) {
				updateForestLayer(tile);
			} else {
				updateMarshLayer(tile);
			}
		}

		private void updateBuildingLayer(Tile tile) {
			// TODO: add goody huts here once they are stored in the save and Tile class
			if (tile.hasBarbarianCamp) {
				setCell(Layer.Building, Atlas.TerrainBuilding, tile, new Vector2I(2, 0));
			} else {
				eraseCell(Layer.Building, tile);
			}
		}

		// updateFogOfWarLayer returns true if the tile is visible or
 		// semi-visible, indicating other layers should be updated.

		private static int fogOfWarIndex(Tile tile, TileKnowledge tk) {
			if (tk.isTileKnown(tile)) {
				return 0;
			}
			int sum = 0;
			// HACK: edge tiles have missing directions in neighbors map
			if (tile.neighbors.Values.Count != 8) {
				return sum;
			}
			if (tk.isTileKnown(tile.neighbors[TileDirection.NORTH]) || tk.isTileKnown(tile.neighbors[TileDirection.NORTHWEST]) || tk.isTileKnown(tile.neighbors[TileDirection.NORTHEAST])) {
				sum += 1 * 2;
			}
			if (tk.isTileKnown(tile.neighbors[TileDirection.WEST]) || tk.isTileKnown(tile.neighbors[TileDirection.NORTHWEST]) || tk.isTileKnown(tile.neighbors[TileDirection.SOUTHWEST])) {
				sum += 3 * 2;
			}
			if (tk.isTileKnown(tile.neighbors[TileDirection.EAST]) || tk.isTileKnown(tile.neighbors[TileDirection.NORTHEAST]) || tk.isTileKnown(tile.neighbors[TileDirection.SOUTHEAST])) {
				sum += 9 * 2;
			}
			if (tk.isTileKnown(tile.neighbors[TileDirection.SOUTH]) || tk.isTileKnown(tile.neighbors[TileDirection.SOUTHWEST]) || tk.isTileKnown(tile.neighbors[TileDirection.SOUTHEAST])) {
				sum += 27 * 2;
			}
			return sum;
		}

		private bool updateFogOfWarLayer(Tile tile, TileKnowledge tk) {
			if (tk.isTileKnown(tile)) {
				eraseCell(Layer.FogOfWar, tile);
				return true;
			}
			int index = fogOfWarIndex(tile, tk);
			setCell(Layer.FogOfWar, Atlas.FogOfWar, tile, new Vector2I(index % 9, index / 9));
			return index > 0; // partially visible
		}

		public void updateTile(Tile tile, TileKnowledge tk) {
			if (tile == Tile.NONE || tile is null) {
				string msg = tile is null ? "null tile" : "Tile.NONE";
				log.Warning($"attempting to update {msg}");
				return;
			}

			if (tk is not null) {
				bool visible = updateFogOfWarLayer(tile, tk);
				if (!visible) {
					return;
				}
			}

			updateRoadLayer(tile, true);

			if (tile.Resource != C7GameData.Resource.NONE) {
				int index = tile.Resource.Icon;
				Vector2I texCoord = new Vector2I(index % 6, index / 6);
				setCell(Layer.Resource, Atlas.Resource, tile, texCoord);
			} else {
				eraseCell(Layer.Resource, tile);
			}

			if (tile.baseTerrainType.Key == "grassland" && tile.isBonusShield) {
				setCell(Layer.TerrainYield, Atlas.TerrainYield, tile, new Vector2I(0, 3));
			} else {
				eraseCell(Layer.TerrainYield, tile);
			}

			updateTerrainOverlayLayer(tile);

			updateBuildingLayer(tile);
		}

		private void updateGridLayer() {
 			if (showGrid) {
 				foreach (Tile tile in data.map.tiles) {
 					setCell(Layer.Grid, Atlas.Grid, tile, Vector2I.Zero);
 				}
 			} else {
 				tilemap.ClearLayer(Layer.Grid.Index());
 			}
 		}

		public void discoverTile(Tile tile, TileKnowledge tk) {
			HashSet<Tile> update = new HashSet<Tile>(tile.neighbors.Values);
			foreach (Tile n in tile.neighbors.Values) {
				foreach (Tile nn in n.neighbors.Values) {
					update.Add(nn);
				}
			}
			foreach (Tile t in update) {
				updateTile(t, tk);
			}
		}
	}
}
