using Godot;
using C7GameData;
using System;
using Serilog;
using System.Linq;

namespace C7.Map {

	partial class MapView : Node2D {
		private string[,]terrain;
		private TileMap terrainTilemap;
		private TileMap terrainTilemapShadow;
		private TileSet terrainTileset;
		private TileMap tilemap;
		private TileSet tileset;
		private Vector2I tileSize = new Vector2I(128, 64);
		private ILogger log = LogManager.ForContext<MapView>();
		public int worldEdgeRight {get; private set;}
		public int worldEdgeLeft {get; private set;}
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

		private void addUniformOffsetsToAtlasSource(ref TileSetAtlasSource source, int width, int height, Vector2I offset) {
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					source.GetTileData(new Vector2I(x, y), 0).TextureOrigin = offset;
				}
			}
		}

		private TileSetAtlasSource loadForestSource(string relPath, bool jungle = false) {
			TileSetAtlasSource source = new TileSetAtlasSource{
				Texture = Util.LoadTextureFromPCX(relPath),
				TextureRegionSize = forestSize,
			};
			for (int x = 0; x < 6; x++) {
				for (int y = 0; y < 10; y++) {
					if ((y < 4 && !jungle) || (y < 2 && x > 3)) {
						continue; // first 4 rows are for jungle tiles
					}
					if ((y > 3 && y < 6 && x > 3) || (y > 5 && y < 8 && x > 4)) {
						continue; // forest tilemap is shaped like this
					}
					source.CreateTile(new Vector2I(x, y));
					if (y == 1 || y == 2 || y == 4 || y == 5) {
						// offset big textures by 12 pixels
						source.GetTileData(new Vector2I(x, y), 0).TextureOrigin = new Vector2I(0, 12);
					}
				}
			}
			return source;
		}

		public override void _Process(double delta) {
			base._Process(delta);
		}

		private void initializeTileMap() {
			terrainTilemap = new TileMap();
			terrainTilemapShadow = new TileMap();
			terrainTileset = Civ3TerrainTileSet.Generate();
			terrainTilemap.TileSet = terrainTileset;
			terrainTilemap.Position += Vector2I.Right * (tileSize.X / 2);
			terrainTilemapShadow.TileSet = terrainTileset;
			terrainTilemapShadow.Position = terrainTilemap.Position + (Vector2I.Left * tileSize.X * width);

			tilemap = new TileMap{ YSortEnabled = true };
			tileset = TileSetLoader.LoadCiv3TileSet();
			tilemap.TileSet = tileset;
			// TileSetAtlasSource roads = loadAtlasSource("Art/Terrain/roads.pcx", tileSize, 16, 16);
			// TileSetAtlasSource rails = loadAtlasSource("Art/Terrain/railroads.pcx", tileSize, 16, 16);

			TileSetAtlasSource resources = new TileSetAtlasSource{
				Texture = Util.LoadTextureFromPCX("Conquests/Art/resources.pcx"),
				TextureRegionSize = resourceSize,
			};
			for (int y = 0; y < 5; y++) {
				for (int x = 0; x < 6; x++) {
					if (y == 4 && x > 1) {
						continue;
					}
					resources.CreateTile(new Vector2I(x, y));
				}
			}

			TileSetAtlasSource tnt = loadAtlasSource("Art/Terrain/tnt.pcx", tileSize, 3, 6);

			TileSetAtlasSource rivers = loadAtlasSource("Art/Terrain/mtnRivers.pcx", tileSize, 4, 4);

			TileSetAtlasSource hills = loadAtlasSource("Art/Terrain/xhills.pcx", hillSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref hills, 4, 4, new Vector2I(0, 4));
			TileSetAtlasSource forestHills = loadAtlasSource("Art/Terrain/hill forests.pcx", hillSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref forestHills, 4, 4, new Vector2I(0, 4));
			TileSetAtlasSource jungleHills = loadAtlasSource("Art/Terrain/hill jungle.pcx", hillSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref jungleHills, 4, 4, new Vector2I(0, 4));

			TileSetAtlasSource mountain = loadAtlasSource("Art/Terrain/Mountains.pcx", mountainSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref mountain, 4, 4, new Vector2I(0, 12));
			TileSetAtlasSource snowMountain = loadAtlasSource("Art/Terrain/Mountains-snow.pcx", mountainSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref snowMountain, 4, 4, new Vector2I(0, 12));
			TileSetAtlasSource forestMountain = loadAtlasSource("Art/Terrain/mountain forests.pcx", mountainSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref forestMountain, 4, 4, new Vector2I(0, 12));
			TileSetAtlasSource jungleMountain = loadAtlasSource("Art/Terrain/mountain jungles.pcx", mountainSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref jungleMountain, 4, 4, new Vector2I(0, 12));
			TileSetAtlasSource volcano = loadAtlasSource("Art/Terrain/Volcanos.pcx", mountainSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref volcano, 4, 4, new Vector2I(0, 12));
			TileSetAtlasSource forestVolcano = loadAtlasSource("Art/Terrain/Volcanos forests.pcx", mountainSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref forestVolcano, 4, 4, new Vector2I(0, 12));
			TileSetAtlasSource jungleVolcano = loadAtlasSource("Art/Terrain/Volcanos jungles.pcx", mountainSize, 4, 4);
			addUniformOffsetsToAtlasSource(ref jungleVolcano, 4, 4, new Vector2I(0, 12));

			TileSetAtlasSource plainsForest = loadForestSource("Art/Terrain/plains forests.pcx");
			TileSetAtlasSource grasslandsForest = loadForestSource("Art/Terrain/grassland forests.pcx", true);
			TileSetAtlasSource tundraForest = loadForestSource("Art/Terrain/tundra forests.pcx");

			tileset.AddSource(roads, Atlas.Road.Index());
			tileset.AddSource(rails, Atlas.Rail.Index());
			tileset.AddSource(resources, Atlas.Resource.Index());
			tileset.AddSource(tnt, Atlas.TerrainYield.Index());
			tileset.AddSource(rivers, Atlas.River.Index());
			tileset.AddSource(hills, Atlas.Hill.Index());
			tileset.AddSource(forestHills, Atlas.ForestHill.Index());
			tileset.AddSource(jungleHills, Atlas.JungleHill.Index());
			tileset.AddSource(mountain, Atlas.Mountain.Index());
			tileset.AddSource(snowMountain, Atlas.SnowMountain.Index());
			tileset.AddSource(forestMountain, Atlas.ForestMountain.Index());
			tileset.AddSource(jungleMountain, Atlas.JungleMountain.Index());
			tileset.AddSource(plainsForest, Atlas.PlainsForest.Index());
			tileset.AddSource(grasslandsForest, Atlas.GrasslandsForest.Index());
			tileset.AddSource(tundraForest, Atlas.TundraForest.Index());
			tileset.AddSource(volcano, Atlas.Volcano.Index());
			tileset.AddSource(forestVolcano, Atlas.ForestVolcano.Index());
			tileset.AddSource(jungleVolcano, Atlas.JungleVolcano.Index());

			// create tilemap layers
			foreach (Layer layer in Enum.GetValues(typeof(Layer))) {
				if (layer != Layer.Invalid) {
					tilemap.AddLayer(layer.Index());
				}
			}

			tilemap.ZIndex = 10; // need to figure out a good way to order z indices
			AddChild(tilemap);
			AddChild(terrainTilemap);
			AddChild(terrainTilemapShadow);
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
			for (int y = 0; y < height; y++) {
				Vector2I cell = new Vector2I(-1, y);
				int x = width - 1;
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

		void setTerrainTile(Vector2I cell, int atlas, Vector2I texCoords) {
			terrainTilemap.SetCell(0, cell, atlas, texCoords);
			terrainTilemapShadow.SetCell(0, cell, atlas, texCoords);
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

		private void setCell(Layer layer, Atlas atlas, Tile tile, Vector2I atlasCoords) {
			if (!tileset.HasSource(atlas.Index())) {
				log.Warning($"atlas id {atlas} is not a valid tileset source");
			}
			if (!tileset.GetSource(atlas.Index()).HasTile(atlasCoords)) {
				log.Warning($"atlas id {atlas} does not have tile at {atlasCoords}");
			}
			tilemap.SetCell(layer.Index(), stackedCoords(tile), atlas.Index(), atlasCoords);
		}

		private void eraseCell(Layer layer, Tile tile) {
			tilemap.EraseCell(layer.Index(), stackedCoords(tile));
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

		private Vector2I roadIndexTo2D(int index) {
			return new Vector2I(index & 0xF, index >> 4);
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
					bool small = tile.getEdgeNeighbors().Any(t => t.IsWater());
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
				if (tile.getEdgeNeighbors().Any(t => t.IsWater())) {
					(row, col) = (2 + tile.xCoordinate % 2, 1 + (tile.xCoordinate % 5));
				}
				setCell(Layer.TerrainOverlay, Atlas.GrasslandsForest, tile, new Vector2I(col, row));
			}
		}

		private bool isForest(Tile tile) {
			return tile.overlayTerrainType.Key == "forest" || tile.overlayTerrainType.Key == "jungle";
		}

		private void updateTerrainOverlayLayer(Tile tile) {
			if (!tile.overlayTerrainType.isHilly() && !isForest(tile)) {
				eraseCell(Layer.TerrainOverlay, tile);
				return;
			}
			if (tile.overlayTerrainType.isHilly()) {
				updateHillLayer(tile);
			} else {
				updateForestLayer(tile);
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
		}
	}
}
