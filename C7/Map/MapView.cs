using Godot;
using C7GameData;
using System;
using Serilog;

namespace C7.Map {

	public enum MapLayer {
		Road,
		Rail,
		Resource,
		TerrainYield,
		River,
		Invalid,
	};

	public static class MapLayerMethods {
		public static (int, int) LayerAndAtlas(this MapLayer mapLayer) {
			return mapLayer switch {
				// (layer, atlas)
				// TODO: figure out how many can go on the same layer?
				MapLayer.River => (0, 0),
				MapLayer.Road => (1, 1),
				MapLayer.Rail => (2, 2),
				MapLayer.Resource => (3, 3),
				MapLayer.TerrainYield => (4, 4),
				MapLayer.Invalid => throw new ArgumentException("MapLayer.Invalid has no tilemap layer"),
				_ => throw new ArgumentException($"unknown MapLayer enum value: ${mapLayer}"),
			};
		}
		public static int Layer(this MapLayer mapLayer) {
			(int layer, _) = mapLayer.LayerAndAtlas();
			return layer;
		}

		public static int Atlas(this MapLayer mapLayer) {
			(_, int atlas) = mapLayer.LayerAndAtlas();
			return atlas;
		}
	};

	partial class MapView : Node2D {
		private string[,]terrain;
		private TileMap terrainTilemap;
		private TileSet terrainTileset;
		private TileMap tilemap;
		private TileSet tileset;
		private Vector2I tileSize = new Vector2I(128, 64);
		private Vector2I resourceSize = new Vector2I(50, 50);
		private ILogger log = LogManager.ForContext<MapView>();

		private int width;
		private int height;
		private GameMap gameMap;

		private TileSet makeTileSet() {
			return new TileSet {
				TileShape = TileSet.TileShapeEnum.Isometric,
				TileLayout = TileSet.TileLayoutEnum.Stacked,
				TileOffsetAxis = TileSet.TileOffsetAxisEnum.Horizontal,
				TileSize = tileSize,
			};
		}

		private void addUniformTilesToAtlasSource(ref TileSetAtlasSource source, int width, int height) {
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					source.CreateTile(new Vector2I(x, y));
				}
			}
		}

		private TileSetAtlasSource loadAtlasSource(string relPath, Vector2I tileSize, int width, int height) {
			TileSetAtlasSource source = new TileSetAtlasSource{
				Texture = Util.LoadTextureFromPCX(relPath),
				TextureRegionSize = tileSize,
			};
			addUniformTilesToAtlasSource(ref source, width, height);
			return source;
		}

		private void initializeTileMap() {
			terrainTilemap = new TileMap();
			terrainTileset = Civ3TerrainTileSet.Generate();
			terrainTilemap.TileSet = terrainTileset;
			terrainTilemap.Position += Vector2I.Right * (tileSize.X / 2);

			tilemap = new TileMap();
			tileset = makeTileSet();
			tilemap.TileSet = tileset;
			TileSetAtlasSource roads = loadAtlasSource("Art/Terrain/roads.pcx", tileSize, 16, 16);
			TileSetAtlasSource rails = loadAtlasSource("Art/Terrain/railroads.pcx", tileSize, 16, 16);

			TileSetAtlasSource resources = new TileSetAtlasSource{
				Texture = Util.LoadTextureFromPCX("Art/resources.pcx"),
				TextureRegionSize = resourceSize,
			};
			for (int y = 0; y < 4; y++) {
				for (int x = 0; x < 6; x++) {
					if (x == 4 && y == 3) {
						break;
					}
					resources.CreateTile(new Vector2I(x, y));
				}
			}

			TileSetAtlasSource tnt = loadAtlasSource("Art/Terrain/tnt.pcx", tileSize, 3, 6);
			TileSetAtlasSource rivers = loadAtlasSource("Art/Terrain/mtnRivers.pcx", tileSize, 4, 4);

			tileset.AddSource(roads, MapLayer.Road.Atlas());
			tileset.AddSource(rails, MapLayer.Rail.Atlas());
			tileset.AddSource(resources, MapLayer.Resource.Atlas());
			tileset.AddSource(tnt, MapLayer.TerrainYield.Atlas());
			tileset.AddSource(rivers, MapLayer.River.Atlas());

			// create tilemap layers
			foreach (MapLayer mapLayer in Enum.GetValues(typeof(MapLayer))) {
				if (mapLayer != MapLayer.Invalid) {
					tilemap.AddLayer(mapLayer.Layer());
				}
			}

			tilemap.ZIndex = 10; // need to figure out a good way to order z indices
			AddChild(tilemap);
			AddChild(terrainTilemap);
		}

		private void setTerrainTiles() {
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					Vector2I cell = new Vector2I(x, y);
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
					string[] corner = new string[4]{top, right, bottom, left};
					TerrainPcx pcx = Civ3TerrainTileSet.GetPcxFor(corner);
					Vector2I texCoords = pcx.getTextureCoords(corner);
					setTerrainTile(cell, pcx.atlas, texCoords);
				}
			}
		}

		void setTerrainTile(Vector2I cell, int atlas, Vector2I texCoords) {
			terrainTilemap.SetCell(0, cell, atlas, texCoords);
		}

		void setTile(Vector2I cell, int atlas, Vector2I texCoords) {
			tilemap.SetCell(0, cell, atlas, texCoords);
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
			gameMap = data.map;
			width = gameMap.numTilesWide / 2;
			height = gameMap.numTilesTall;
			initializeTileMap();
			terrain = new string[width, height];

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

			foreach (Tile tile in gameMap.tiles) {
				updateTile(tile);
			}

			// temp but place units in current position
			foreach (Tile tile in gameMap.tiles) {
				if (tile.unitsOnTile.Count > 0) {
					MapUnit unit = tile.unitsOnTile[0];
					UnitSprite sprite = new UnitSprite(game.civ3AnimData);
					MapUnit.Appearance appearance = game.animTracker.getUnitAppearance(unit);

					var coords = stackedCoords(tile);
					sprite.Position = tilemap.MapToLocal(coords);

					game.civ3AnimData.forUnit(unit.unitType, appearance.action).loadSpriteAnimation();
					string animName = AnimationManager.AnimationKey(unit.unitType, appearance.action, appearance.direction);
					sprite.SetAnimation(animName);
					sprite.SetFrame(0);
					AddChild(sprite);
				}
			}
		}

		public Tile tileAt(GameMap gameMap, Vector2 globalMousePosition) {
			Vector2I tilemapCoord = tilemap.LocalToMap(ToLocal(globalMousePosition));
			(int x, int y) = unstackedCoords(tilemapCoord);
			return gameMap.tileAt(x, y);
		}

		private Vector2I roadIndexTo2D(int index) {
			return new Vector2I(index & 0xF, index >> 4);
		}

		private void setCell(MapLayer mapLayer, Vector2I coords, Vector2I atlasCoords) {
			(int layer, int atlas) = mapLayer.LayerAndAtlas();
			if (!tileset.HasSource(atlas)) {
				log.Warning($"atlas id {atlas} is not a valid tileset source");
			}
			tilemap.SetCell(layer, coords, atlas, atlasCoords);
		}

		private void setCell(MapLayer mapLayer, Tile tile, Vector2I atlasCoords) {
			setCell(mapLayer, stackedCoords(tile), atlasCoords);
		}

		private void eraseCell(MapLayer mapLayer, Vector2I coords) {
			int layer = mapLayer.Layer();
			tilemap.EraseCell(layer, coords);
		}

		private void eraseCell(MapLayer mapLayer, Tile tile) {
			eraseCell(mapLayer, stackedCoords(tile));
		}

		private void updateRoadLayer(Tile tile, bool center) {
			if (!tile.overlays.road) {
				eraseCell(MapLayer.Road, tile);
				eraseCell(MapLayer.Rail, tile);
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
				eraseCell(MapLayer.Rail, tile);
				setCell(MapLayer.Road, tile, roadIndexTo2D(index));
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
					setCell(MapLayer.Road, tile, roadIndexTo2D(roadIndex));
				} else {
					eraseCell(MapLayer.Road, tile);
				}
				setCell(MapLayer.Rail, tile, roadIndexTo2D(railIndex));
			}

			if (center) {
				// updating a tile may change neighboring tiles
				foreach (Tile neighbor in tile.neighbors.Values) {
					updateRoadLayer(neighbor, false);
				}
			}
		}

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
				eraseCell(MapLayer.River, tile);
			} else {
				setCell(MapLayer.River, tile, new Vector2I(index % 4, index / 4));
			}
		}

		public void updateTile(Tile tile) {
			if (tile == Tile.NONE || tile is null) {
				string msg = tile is null ? "null tile" : "Tile.NONE";
				log.Warning($"attempting to update {msg}");
				return;
			}

			updateRoadLayer(tile, true);

			if (tile.Resource != C7GameData.Resource.NONE) {
				int index = tile.Resource.Index;
				Vector2I texCoord = new Vector2I(index % 6, index / 6);
				setCell(MapLayer.Resource, tile, texCoord);
			} else {
				eraseCell(MapLayer.Resource, tile);
			}

			if (tile.baseTerrainType.Key == "grassland" && tile.isBonusShield) {
				setCell(MapLayer.TerrainYield, tile, new Vector2I(0, 3));
			} else {
				eraseCell(MapLayer.TerrainYield, tile);
			}

			updateRiverLayer(tile);
		}

	}
}
