using Godot;
using System.Collections.Generic;
using C7GameData;
using System;

namespace C7.Map {

	public enum MapLayer {
		Road,
		Rail,
		Invalid,
	};

	public static class MapLayerMethods {
		public static (int, int) LayerAndAtlas(this MapLayer mapLayer) {
			return mapLayer switch {
				MapLayer.Road => (0, 0),
				MapLayer.Rail => (1, 1),
				MapLayer.Invalid => throw new ArgumentException("MapLayer.Invalid has no tilemap layer"),
				_ => throw new ArgumentException($"unknown MapLayer enum value: ${mapLayer}"),
			};
		}
		public static int Layer(this MapLayer mapLayer) {
			(int layer, _) = mapLayer.LayerAndAtlas();
			return layer;
		}
	};

	partial class MapView : Node2D {
		private string[,]terrain;
		private TileMap terrainTilemap;
		private TileSet terrainTileset;
		private TileMap tilemap;
		private TileSet tileset;
		private Vector2I tileSize = new Vector2I(128, 64);
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

			// the order in which these are added determines their atlas ID
			// TODO: associate pcx file(s) with MapLayer enum to ensure they
			// are added in the correct order wrt their atlas ID
			tileset.AddSource(roads);
			tileset.AddSource(rails);
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

			foreach (C7GameData.Tile t in gameMap.tiles) {
				Vector2I coords = stackedCoords(t);
				terrain[coords.X, coords.Y] = t.baseTerrainTypeKey;
			}
			setTerrainTiles();

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
			tilemap.SetCell(layer, coords, atlas, atlasCoords);
		}

		private void updateRoadLayer(Tile tile, bool center) {
			if (!tile.overlays.road) {
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
				setCell(MapLayer.Road, stackedCoords(tile), roadIndexTo2D(index));
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
					setCell(MapLayer.Road, stackedCoords(tile), roadIndexTo2D(roadIndex));
				}
				setCell(MapLayer.Rail, stackedCoords(tile), roadIndexTo2D(railIndex));
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

		public void updateTile(Tile tile) {
			// update terrain ?
			if (tile.overlays.road) {
				updateRoadLayer(tile, true);
			}
		}

	}
}
