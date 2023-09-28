using C7GameData;
using ConvertCiv3Media;
using Godot;

namespace C7.Map {
	public partial class FogOfWarLayer : LooseLayer {

		private readonly ImageTexture fogOfWarTexture;
		private readonly Vector2 tileSize;

		public FogOfWarLayer() {
			Pcx fogOfWarPcx = new Pcx(Util.Civ3MediaPath("Art/Terrain/FogOfWar.pcx"));
			fogOfWarTexture = PCXToGodot.getPureAlphaFromPCX(fogOfWarPcx);
			tileSize = fogOfWarTexture.GetSize() / 9;
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			Rect2 screenTarget = new Rect2(tileCenter - tileSize / 2, tileSize);
			TileKnowledge tileKnowledge = gameData.GetHumanPlayers()[0].tileKnowledge;
			//N.B. FogOfWar.pcx handles both totally unknown and fogged tiles, indexed in the same file.
			//Hence the trinary math rather than the more commonplace binary.
			if (!tileKnowledge.isTileKnown(tile)) {
				int sum = 0;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTH]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHWEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHEAST]))
					sum += 1 * 2;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.WEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHWEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHWEST]))
					sum += 3 * 2;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.EAST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHEAST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHEAST]))
					sum += 9 * 2;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTH]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHWEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHEAST]))
					sum += 27 * 2;
				if (sum != 0) {
					looseView.DrawTextureRectRegion(fogOfWarTexture, screenTarget, getRect(sum));
				}
			}
			//do nothing if the tile is known (equiv to the lower-right)
		}

		private Rect2 getRect(int sum) {
			int row = sum / 9;
			int col = sum % 9;
			return new Rect2(col * tileSize.X, row * tileSize.Y, tileSize);
		}
	}
}
