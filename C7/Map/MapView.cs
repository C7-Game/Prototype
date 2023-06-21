using Godot;
using System.Collections.Generic;
using C7GameData;
using System;

namespace C7.Map {

	partial class MapView : Node2D {
		// same order as terrainPcxList
		private List<string> terrainPcxFiles = new List<string> {
			"Art/Terrain/xtgc.pcx", "Art/Terrain/xpgc.pcx", "Art/Terrain/xdgc.pcx",
			"Art/Terrain/xdpc.pcx", "Art/Terrain/xdgp.pcx", "Art/Terrain/xggc.pcx",
			"Art/Terrain/wCSO.pcx", "Art/Terrain/wSSS.pcx", "Art/Terrain/wOOO.pcx",
		};
		// same order as terrainPcxFiles
		private List<TerrainPcx> terrainPcxList = new List<TerrainPcx>() {
			new TerrainPcx("tgc", new string[]{"tundra", "grassland", "coast"}, 0),
			new TerrainPcx("pgc", new string[]{"plains", "grassland", "coast"}, 1),
			new TerrainPcx("dgc", new string[]{"desert", "grassland", "coast"}, 2),
			new TerrainPcx("dpc", new string[]{"desert", "plains", "coast"}, 3),
			new TerrainPcx("dgp", new string[]{"desert", "grassland", "plains"}, 4),
			new TerrainPcx("ggc", new string[]{"grassland", "grassland", "coast"}, 5),
			new TerrainPcx("cso", new string[]{"coast", "sea", "ocean"}, 6),
			new TerrainPcx("sss", new string[]{"sea", "sea", "sea"}, 7),
			new TerrainPcx("ooo", new string[]{"ocean", "ocean", "ocean"}, 8),
		};
		private string[,]terrain;
		private TileMap terrainTilemap;
		private TileSet terrainTileset;
		private TileMap tilemap;
		private TileSet tileset;
		private List<ImageTexture> textures;
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

		private void initializeTileMap() {
			this.terrainTilemap = new TileMap();
			terrainTilemap.TextureFilter = TextureFilterEnum.Nearest;
			terrainTileset = makeTileSet();

			// register 9 x 9 layout of tiles in each terrain pcx
			foreach (ImageTexture texture in textures) {
				TileSetAtlasSource source = new TileSetAtlasSource();
				source.Texture = texture;
				source.TextureRegionSize = tileSize;
				addUniformTilesToAtlasSource(ref source, 9, 9);
				terrainTileset.AddSource(source);
			}
			terrainTilemap.TileSet = terrainTileset;
			terrainTilemap.Position += Vector2I.Right * (width / 2);

			tilemap = new TileMap();
			tileset = makeTileSet();
			tilemap.TileSet = tileset;
			TileSetAtlasSource roads = new TileSetAtlasSource{
				Texture = Util.LoadTextureFromPCX("Art/Terrain/roads.pcx"),
				TextureRegionSize = tileSize,
			};
			addUniformTilesToAtlasSource(ref roads, 16, 16);
			TileSetAtlasSource rails = new TileSetAtlasSource{
				Texture = Util.LoadTextureFromPCX("Art/Terrain/railroads.pcx"),
				TextureRegionSize = tileSize,
			};
			tileset.AddSource(roads);
			tileset.AddSource(rails);

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
					TerrainPcx pcx = terrainPcxList.Find(pcx => pcx.validFor(corner));
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

		private Vector2I stackedCoords(int x, int y) {
			x = y % 2 == 0 ? x / 2 : (x - 1) / 2;
			return new Vector2I(x, y);
		}

		private (int, int) unstackedCoords(Vector2I stacked) {
			(int x, int y) = (stacked.X, stacked.Y);
			x = y % 2 == 0 ? x * 2 : (x * 2) + 1;
			return (x, y);
		}

		public MapView(Game game, GameData data) {
			textures = terrainPcxFiles.ConvertAll(path => Util.LoadTextureFromPCX(path));
			gameMap = data.map;
			width = gameMap.numTilesWide / 2;
			height = gameMap.numTilesTall;
			initializeTileMap();
			terrain = new string[width, height];

			foreach (C7GameData.Tile t in gameMap.tiles) {
				Vector2I coords = stackedCoords(t.xCoordinate, t.yCoordinate);
				terrain[coords.X, coords.Y] = t.baseTerrainTypeKey;
			}
			setTerrainTiles();

			// temp but place units in current position
			foreach (Tile tile in gameMap.tiles) {
				if (tile.unitsOnTile.Count > 0) {
					MapUnit unit = tile.unitsOnTile[0];
					UnitSprite sprite = new UnitSprite(game.civ3AnimData);
					MapUnit.Appearance appearance = game.animTracker.getUnitAppearance(unit);

					var coords = stackedCoords(tile.xCoordinate, tile.yCoordinate);
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
			int column = index & 0xF;
			int row = index >> 4;
			return new Vector2I(column, row);
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
				setTile(stackedCoords(tile.xCoordinate, tile.yCoordinate), 0, roadIndexTo2D(index));
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
					setTile(stackedCoords(tile.xCoordinate, tile.yCoordinate), 0, roadIndexTo2D(roadIndex));
				}
				// TODO: this needs to go on a different layer in the tilemap
				setTile(stackedCoords(tile.xCoordinate, tile.yCoordinate), 1, roadIndexTo2D(railIndex));
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
