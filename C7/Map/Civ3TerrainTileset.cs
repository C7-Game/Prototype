using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C7.Map {

	class TerrainPcx {
		private static Random prng = new Random();
		private string name;
		// abc refers to the layout of the terrain tiles in the pcx based on
		// the positions of each terrain at the corner of 4 tiles.
		// - https://forums.civfanatics.com/threads/terrain-editing.622999/
		// - https://forums.civfanatics.com/threads/editing-terrain-pcx-files.102840/
		private string[] abc;
		public int atlas;
		public TerrainPcx(string name, string[] abc, int atlas) {
			this.name = name;
			this.abc = abc;
			this.atlas = atlas;
		}
		public bool validFor(string[] corner) {
			return corner.All(tile => abc.Contains(tile));
		}
		private int abcIndex(string terrain) {
			List<int> indices = new List<int>();
			for (int i = 0; i < abc.Count(); i++) {
				if (abc[i] == terrain) {
					indices.Add(i);
				}
			}
			return indices[prng.Next(indices.Count)];
		}

		// getTextureCoords looks up the correct texture index in the pcx
		// for the given position of each corner terrain type
		public Vector2I getTextureCoords(string[] corner) {
			int top = abcIndex(corner[0]);
			int right = abcIndex(corner[1]);
			int bottom = abcIndex(corner[2]);
			int left = abcIndex(corner[3]);
			int index = top + (left * 3) + (right * 9) + (bottom * 27);
			return new Vector2I(index % 9, index / 9);
		}
	}

	// Civ3TerrainTileSet loads civ3 terrain pcx files and generates a tileset
	class Civ3TerrainTileSet {
		// same order as terrainPcxList
		private static readonly List<string> terrainPcxFiles = new List<string> {
			"Art/Terrain/xtgc.pcx", "Art/Terrain/xpgc.pcx", "Art/Terrain/xdgc.pcx",
			"Art/Terrain/xdpc.pcx", "Art/Terrain/xdgp.pcx", "Art/Terrain/xggc.pcx",
			"Art/Terrain/wCSO.pcx", "Art/Terrain/wSSS.pcx", "Art/Terrain/wOOO.pcx",
		};

		// same order as terrainPcxFiles
		private static readonly List<TerrainPcx> terrainPcxList = new List<TerrainPcx>() {
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

		private static readonly Vector2I terrainTileSize = new Vector2I(128, 64);

		public static TileSet Generate() {
			List<ImageTexture> textures = terrainPcxFiles.ConvertAll(path => Util.LoadTextureFromPCX(path));
			TileSet tileset = new TileSet{
				TileShape = TileSet.TileShapeEnum.Isometric,
				TileLayout = TileSet.TileLayoutEnum.Stacked,
				TileOffsetAxis = TileSet.TileOffsetAxisEnum.Horizontal,
				TileSize = terrainTileSize,
			};
			foreach (ImageTexture texture in textures) {
				TileSetAtlasSource source = new TileSetAtlasSource{
					Texture = texture,
					TextureRegionSize = terrainTileSize,
				};
				for (int x = 0; x < 9; x++) {
					for (int y = 0; y < 9; y++) {
						source.CreateTile(new Vector2I(x, y));
					}
				}
				tileset.AddSource(source);
			}
			return tileset;
		}

		public static TerrainPcx GetPcxFor(string[] corners) {
			if (corners.Length != 4) {
				throw new ArgumentException($"terrain corner must be of 4 tiles but got {corners.Length}");
			}
			return terrainPcxList.Find(pcx => pcx.validFor(corners));
		}

	}

}
