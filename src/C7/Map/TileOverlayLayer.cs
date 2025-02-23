using System;
using System.Collections.Generic;
using C7GameData;
using Godot;

namespace C7.Map {
	public partial class TileOverlayLayer : LooseLayer {
		private readonly ImageTexture roadTexture;
		private readonly ImageTexture railroadTexture;
		private readonly ImageTexture mineTexture;
		private readonly ImageTexture grassIrrigationTexture;
		private readonly ImageTexture desertIrrigationTexture;
		private readonly ImageTexture plainsIrrigationTexture;
		private readonly ImageTexture tundraIrrigationTexture;
		private readonly Vector2 tileSize;

		public TileOverlayLayer() {
			roadTexture = Util.LoadTextureFromPCX("Art/Terrain/roads.pcx");
			railroadTexture = Util.LoadTextureFromPCX("Art/Terrain/railroads.pcx");
			tileSize = roadTexture.GetSize() / 16;
			// grid 16x16 tiles
			// assume that roads and railroads textures have the same size

			// TerrainBuildings.pcx contains multiple pieces of art in a grid, with each
			// item being 128x64 pixesl.
			//
			// The basic version is:
			//  Fortress (ancient)     | Colony (an)   | Barb camp
			//  Fortress (medieval)    | Colony (me)   | Mine
			//  Fortress (industrial)  | Colony (in)   | Empty
			//  Fortress (modern)      | Colony (mo)   | Empty
			mineTexture = Util.LoadTextureFromPCX("Art/Terrain/TerrainBuildings.pcx", 128 * 2, 64, 128, 64);

			// Each irrigation.pcx has a 4x4 grid of irrigation tiles, with
			// each tile being 128x64 pixels.
			grassIrrigationTexture = Util.LoadTextureFromPCX("Art/Terrain/irrigation.pcx");
			desertIrrigationTexture = Util.LoadTextureFromPCX("Art/Terrain/irrigation DESETT.pcx");
			plainsIrrigationTexture = Util.LoadTextureFromPCX("Art/Terrain/irrigation PLAINS.pcx");
			tundraIrrigationTexture = Util.LoadTextureFromPCX("Art/Terrain/irrigation TUNDRA.pcx");
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			if (!HasAnyOverlays(tile)) return;

			Rect2 screenTarget = new Rect2(tileCenter - tileSize / 2, tileSize);

			// Irrigation shows up under roads, so draw that first.
			if (tile.overlays.irrigation) {
				// Figure out which index into the irrigation texture to use for
				// this tile.
				int irrigationIndex = 0;
				foreach (KeyValuePair<TileDirection, Tile> dirToTile in tile.neighbors) {
					if (dirToTile.Value.overlays.irrigation) {
						irrigationIndex |= getIrrigationFlag(dirToTile.Key);
					}
				}

				// Deserts, plains, and tundra (??) have specific textures for
				// irrigation. Everything else uses the grassland texture.
				ImageTexture texture;
				if (tile.baseTerrainType.Key == "plains") {
					texture = plainsIrrigationTexture;
				} else if (tile.baseTerrainType.Key == "desert") {
					texture = desertIrrigationTexture;
				} else if (tile.baseTerrainType.Key == "tundra") {
					texture = tundraIrrigationTexture;
				} else {
					texture = grassIrrigationTexture;
				}

				// Draw the subtexture of the irrigation texture for this tile.
				looseView.DrawTextureRectRegion(texture, screenTarget, getIrrigationRect(irrigationIndex));
			}

			if (hasRoad(tile) && !hasRailRoad(tile)) {
				int roadIndex = 0;
				foreach (KeyValuePair<TileDirection, Tile> dirToTile in tile.neighbors) {
					if (hasRoad(dirToTile.Value)) {
						roadIndex |= getRoadFlag(dirToTile.Key);
					}
				}
				looseView.DrawTextureRectRegion(roadTexture, screenTarget, getRoadRect(roadIndex));
			}

			if (hasRailRoad(tile)) {
				int roadIndex = 0;
				int railroadIndex = 0;
				foreach (KeyValuePair<TileDirection, Tile> dirToTile in tile.neighbors) {
					if (hasRailRoad(dirToTile.Value)) {
						railroadIndex |= getRoadFlag(dirToTile.Key);
					} else if (hasRoad(dirToTile.Value)) {
						roadIndex |= getRoadFlag(dirToTile.Key);
					}
				}
				if (roadIndex != 0) {
					looseView.DrawTextureRectRegion(roadTexture, screenTarget, getRoadRect(roadIndex));
				}
				looseView.DrawTextureRectRegion(railroadTexture, screenTarget, getRoadRect(railroadIndex));
			}

			// Mines go over roads, so draw those last.
			if (tile.overlays.mine) {
				looseView.DrawTexture(mineTexture, screenTarget.Position);
			}
		}

		// Returns the rectangle within the road texture for a given index,
		// where the index has been constructed by OR'ing together the direction
		// flags for adjacent roads.
		private Rect2 getRoadRect(int index) {
			int row = index >> 4;
			int column = index & 0xF;
			return new Rect2(column * tileSize.X, row * tileSize.Y, tileSize);
		}

		// Like above, but for irrigation.
		private Rect2 getIrrigationRect(int index) {
			// The index is set up so that the layout looks like
			//
			//  0  1  2  3
			//  4  5  6  7
			//  ...
			int row = index / 4;
			int column = index % 4;
			return new Rect2(column * tileSize.X, row * tileSize.Y, tileSize);
		}

		// The per-neighbor index values that can be OR'd together to get the
		// proper rectangle within the road/railroad texture.
		private static int getRoadFlag(TileDirection direction) {
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

		// Like getRoadFlag, but for irrigation, which only depends on the
		// diagonal neighbors.
		//
		// Index values taken from ClassicRenderer.java in
		// https://hg.sr.ht/~adj/civ3_cross_platform_editor.
		private static int getIrrigationFlag(TileDirection direction) {
			return direction switch {
				TileDirection.NORTHWEST => 0x1,
				TileDirection.NORTHEAST => 0x2,
				TileDirection.SOUTHWEST => 0x4,
				TileDirection.SOUTHEAST => 0x8,
				TileDirection.EAST => 0,
				TileDirection.SOUTH => 0,
				TileDirection.WEST => 0,
				TileDirection.NORTH => 0,
				_ => throw new ArgumentOutOfRangeException("Invalid TileDirection")
			};
		}

		private static bool HasAnyOverlays(Tile tile) {
			return tile.overlays.road ||
				tile.overlays.railroad ||
				tile.overlays.mine ||
				tile.overlays.irrigation;
		}

		private static bool hasRoad(Tile tile) {
			return tile.overlays.road;
		}

		private static bool hasRailRoad(Tile tile) {
			return tile.overlays.railroad;
		}
	}
}
