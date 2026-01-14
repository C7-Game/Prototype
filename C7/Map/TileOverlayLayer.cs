using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using Godot;

namespace C7.Map {
	public partial class TileOverlayLayer : LooseLayer {
		private readonly ImageTexture roadTexture;
		private readonly ImageTexture railroadTexture;
		private readonly ImageTexture grassIrrigationTexture;
		private readonly ImageTexture desertIrrigationTexture;
		private readonly ImageTexture plainsIrrigationTexture;
		private readonly ImageTexture tundraIrrigationTexture;
		private readonly Vector2 tileSize;

		public TileOverlayLayer() {
			roadTexture = TextureLoader.Load("terrain_improvements.road");
			railroadTexture = TextureLoader.Load("terrain_improvements.railroad");
			tileSize = roadTexture.GetSize() / 16;
			// grid 16x16 tiles
			// assume that roads and railroads textures have the same size

			// Each irrigation.pcx has a 4x4 grid of irrigation tiles, with
			// each tile being 128x64 pixels.
			grassIrrigationTexture = TextureLoader.Load("terrain_improvements.irrigation.grass");
			desertIrrigationTexture = TextureLoader.Load("terrain_improvements.irrigation.desert");
			plainsIrrigationTexture = TextureLoader.Load("terrain_improvements.irrigation.plains");
			tundraIrrigationTexture = TextureLoader.Load("terrain_improvements.irrigation.tundra");
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			Rect2 screenTarget = new Rect2(tileCenter - tileSize / 2, tileSize);

			foreach (TerrainImprovement ti in tile.overlays.GetImprovements().OrderBy(ti => ti.zIndex)) {
				switch (ti.key) {
					case "irrigation":
						drawIrrigaton(looseView, tile, screenTarget);
						break;
					case "road":
						drawRoad(looseView, tile, screenTarget);
						break;
					case "railroad":
						drawRailRoad(looseView, tile, screenTarget);
						break;
					default:
						looseView.DrawTexture(TextureLoader.Load($"terrain_improvements.{ti.key}"), screenTarget.Position);
						break;
				}
			}
		}

		private void drawIrrigaton(LooseView looseView, Tile tile, Rect2 screenTarget) {
			// Figure out which index into the irrigation texture to use for
			// this tile.
			int irrigationIndex = 0;
			foreach (KeyValuePair<TileDirection, Tile> dirToTile in tile.neighbors) {
				if (hasIrrigation(dirToTile.Value)) {
					irrigationIndex |= getIrrigationFlag(dirToTile.Key);
				}
			}

			// Deserts, plains, and tundra (??) have specific textures for
			// irrigation. Everything else uses the grassland texture.
			ImageTexture texture = tile.baseTerrainType.Key switch {
				"plains" => plainsIrrigationTexture,
				"desert" => desertIrrigationTexture,
				"tundra" => tundraIrrigationTexture,
				_ => grassIrrigationTexture
			};

			// Draw the subtexture of the irrigation texture for this tile.
			looseView.DrawTextureRectRegion(texture, screenTarget, getIrrigationRect(irrigationIndex));
		}

		private void drawRoad(LooseView looseView, Tile tile, Rect2 screenTarget) {
			int roadIndex = 0;
			foreach (KeyValuePair<TileDirection, Tile> dirToTile in tile.neighbors) {
				if (hasRoad(dirToTile.Value) || dirToTile.Value.HasCity || hasRailRoad((dirToTile.Value))) {
					roadIndex |= getRoadFlag(dirToTile.Key);
				}
			}
			looseView.DrawTextureRectRegion(roadTexture, screenTarget, getRoadRect(roadIndex));
		}

		private void drawRailRoad(LooseView looseView, Tile tile, Rect2 screenTarget) {
			int roadIndex = 0;
			int railroadIndex = 0;
			foreach (KeyValuePair<TileDirection, Tile> dirToTile in tile.neighbors) {
				if (hasRailRoad(dirToTile.Value) || dirToTile.Value.HasCity) {
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

		private static bool hasRoad(Tile tile) {
			return tile.overlays.ImprovementAtLayer(TerrainImprovement.Layer.Roads)?.key == "road";
		}

		private static bool hasRailRoad(Tile tile) {
			return tile.overlays.ImprovementAtLayer(TerrainImprovement.Layer.Roads)?.key == "railroad";
		}

		private static bool hasIrrigation(Tile tile) {
			return tile.overlays.ImprovementAtLayer(TerrainImprovement.Layer.ResourceDevelopment)?.key == "irrigation";
		}
	}
}
