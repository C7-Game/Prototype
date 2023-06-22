using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C7.Map {

	public enum Layer {
		TerrainOverlay,
		River,
		Road,
		Rail,
		Resource,
		TerrainYield,
		Invalid,
	};

	public static class LayerExtensions {
		public static int Index(this Layer layer) {
			return (int)layer;
		}
	}

	public enum Atlas {
		Hill,
		ForestHill,
		JungleHill,
		Mountain,
		SnowMountain,
		ForestMountain,
		JungleMountain,
		Volcano,
		ForestVolcano,
		JungleVolcano,
		PlainsForest,
		GrasslandsForest,
		TundraForest,
		River,
		Road,
		Rail,
		Resource,
		TerrainYield,
		Invalid,
	}

	public static class AtlasExtensions {
		public static int Index(this Atlas atlas) {
			return (int)atlas;
		}
	}

	class AtlasLoader {
		string path;
		int width;
		int height;
		Vector2I regionSize;
		Vector2I textureOrigin;
		protected TileSetAtlasSource source;
		bool loaded = false;

		public AtlasLoader(string p, int w, int h, Vector2I rs, int y = 0) {
			path = p;
			width = w;
			height = h;
			regionSize = rs;
			textureOrigin = new Vector2I(0, y);
			source = new TileSetAtlasSource{
				Texture = Util.LoadTextureFromPCX(path),
				TextureRegionSize = regionSize,
			};
		}

		protected void createTile(int x, int y, bool doOffset = true) {
			Vector2I atlasCoords = new Vector2I(x, y);
			source.CreateTile(atlasCoords);
			if (doOffset && textureOrigin.Y != 0) {
				source.GetTileData(atlasCoords, 0).TextureOrigin = textureOrigin;
			}
		}

		public TileSetAtlasSource Load() {
			if (!loaded) {
				load();
				loaded = true;
			}
			return source;
		}

		protected virtual void load() {
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					createTile(x, y);
				}
			}
		}
	}

	class ForestAtlasLoader : AtlasLoader {
		bool jungle;
		public ForestAtlasLoader(string p, Vector2I rs, bool j = false) : base(p, -1, -1, rs, 12) {
			jungle = j;
		}

		protected override void load() {
			for (int x = 0; x < 6; x++) {
				for (int y = 0; y < 10; y++) {
					if ((y < 4 && !jungle) || (y < 2 && x > 3)) {
						continue; // first 4 rows are for jungle tiles
					}
					if ((y > 3 && y < 6 && x > 3) || (y > 5 && y < 8 && x > 4)) {
						continue; // forest tilemap is shaped like this
					}
					bool shouldDoOffset = y == 1 || y == 2 || y == 4 || y == 5;
					createTile(x, y, shouldDoOffset);
				}
			}
		}
	};

	class ResourceAtlasLoader : AtlasLoader {
		public ResourceAtlasLoader(string p, Vector2I rs) : base(p, -1, -1, rs) {}

		protected override void load() {
			for (int y = 0; y < 5; y++) {
				for (int x = 0; x < 6; x++) {
					if (y == 4 && x > 1) {
						continue;
					}
					createTile(x, y);
				}
			}
		}
	}

	// TileSetLoader loads tileset atlas sources
	class TileSetLoader {
		private static readonly Vector2I tileSize = new Vector2I(128, 64);
		private static readonly Vector2I hillSize = new Vector2I(128, 72);
		private static readonly Vector2I mountainSize = new Vector2I(128, 88);
		private static readonly Vector2I volcanoSize = new Vector2I(128, 88);

		private static readonly Vector2I forestSize = new Vector2I(128, 88);
		private static readonly Vector2I resourceSize = new Vector2I(50, 50);

		private static readonly Dictionary<Atlas, AtlasLoader> civ3PcxForAtlas = new Dictionary<Atlas, AtlasLoader> {
			{Atlas.Resource, new ResourceAtlasLoader("Conquests/Art/resources.pcx", resourceSize)},

			{Atlas.Road, new AtlasLoader("Art/Terrain/roads.pcx", 16, 16, tileSize)},
			{Atlas.Rail, new AtlasLoader("Art/Terrain/railroads.pcx", 16, 16, tileSize)},

			{Atlas.TerrainYield, new AtlasLoader("Art/Terrain/tnt.pcx", 3, 6, tileSize)},

			{Atlas.River, new AtlasLoader("Art/Terrain/mtnRivers.pcx", 4, 4, tileSize)},

			{Atlas.Hill, new AtlasLoader("Art/Terrain/xhills.pcx", 4, 4, hillSize, 4)},
			{Atlas.ForestHill, new AtlasLoader("Art/Terrain/hill forests.pcx", 4, 4, hillSize, 4)},
			{Atlas.JungleHill, new AtlasLoader("Art/Terrain/hill jungle.pcx", 4, 4, hillSize, 4)},

			{Atlas.Mountain, new AtlasLoader("Art/Terrain/Mountains.pcx", 4, 4, mountainSize, 12)},
			{Atlas.SnowMountain, new AtlasLoader("Art/Terrain/Mountains-snow.pcx", 4, 4, mountainSize, 12)},
			{Atlas.ForestMountain, new AtlasLoader("Art/Terrain/mountain forests.pcx", 4, 4, mountainSize, 12)},
			{Atlas.JungleMountain, new AtlasLoader("Art/Terrain/mountain jungles.pcx", 4, 4, mountainSize, 12)},

			{Atlas.Volcano, new AtlasLoader("Art/Terrain/Volcanos.pcx", 4, 4, mountainSize, 12)},
			{Atlas.ForestVolcano, new AtlasLoader("Art/Terrain/Volcanos forests.pcx", 4, 4, mountainSize, 12)},
			{Atlas.JungleVolcano, new AtlasLoader("Art/Terrain/Volcanos jungles.pcx", 4, 4, mountainSize, 12)},

			{Atlas.PlainsForest, new ForestAtlasLoader("Art/Terrain/plains forests.pcx", forestSize)},
			{Atlas.GrasslandsForest, new ForestAtlasLoader("Art/Terrain/plains forests.pcx", forestSize, true)},
			{Atlas.TundraForest, new ForestAtlasLoader("Art/Terrain/tundra forests.pcx", forestSize)},
		};

		public static TileSet LoadCiv3TileSet() {
			TileSet tileset = new TileSet {
				TileShape = TileSet.TileShapeEnum.Isometric,
				TileLayout = TileSet.TileLayoutEnum.Stacked,
				TileOffsetAxis = TileSet.TileOffsetAxisEnum.Horizontal,
				TileSize = tileSize,
			};

			foreach ((Atlas atlas, AtlasLoader loader) in civ3PcxForAtlas) {
				TileSetAtlasSource source = loader.Load();
				tileset.AddSource(source, atlas.Index());
			}

			return tileset;
		}
	}
}
