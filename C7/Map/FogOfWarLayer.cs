using C7GameData;
using Godot;

namespace C7.Map {
	public partial class FogOfWarLayer : LooseLayer {

		private readonly ImageTexture fogOfWarTexture;
		private readonly Vector2 tileSize;

		public FogOfWarLayer() {
			fogOfWarTexture = TextureLoader.Load("terrain.fog_of_war");
			tileSize = fogOfWarTexture.GetSize() / 9;
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {

			// Fog of war is computed from the squares' intersections.
			// Thus, for each iteration, we have to consider that we are at the north intersection of the iterated tile

			Tile north = tile.neighbors[TileDirection.NORTH];
			Tile south = tile;
			Tile east = tile.neighbors[TileDirection.NORTHEAST];
			Tile west = tile.neighbors[TileDirection.NORTHWEST];

			TileKnowledge tileKnowledge = gameData.GetFirstHumanPlayer().tileKnowledge;
			int column = 0;
			int row = 0;

			if (tileKnowledge.isActiveTile(north)) {
				column += 2;
			} else if (tileKnowledge.isTileKnown(north)) {
				column += 1;
			}

			if (tileKnowledge.isActiveTile(west)) {
				column += 6;
			} else if (tileKnowledge.isTileKnown(west)) {
				column += 3;
			}

			if (tileKnowledge.isActiveTile(east)) {
				row += 2;
			} else if (tileKnowledge.isTileKnown(east)) {
				row += 1;
			}

			if (tileKnowledge.isActiveTile(south)) {
				row += 6;
			} else if (tileKnowledge.isTileKnown(south)) {
				row += 3;
			}

			// save a few draw calls when the tile is completely visible
			if (column == 8 && row == 8) {
				return;
			}

			var fogOrigin = new Vector2(tileCenter.X - tileSize.X/2, tileCenter.Y - tileSize.Y);
			var fogRect = new Rect2(fogOrigin, tileSize);
			var spriteRect = new Rect2(column * tileSize.X, row * tileSize.Y, tileSize * 0.999f);
			looseView.DrawTextureRectRegion(fogOfWarTexture, fogRect, spriteRect);
		}
	}
}
