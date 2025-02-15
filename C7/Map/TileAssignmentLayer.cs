using System;
using C7GameData;
using Godot;

namespace C7.Map {
	public partial class TileAssignmentLayer : LooseLayer {
		ImageTexture foodTexture = Util.LoadTextureFromPCX("Art/city screen/CityIcons.pcx", 195, 1, 21, 30);
		ImageTexture shieldTexture = Util.LoadTextureFromPCX("Art/city screen/CityIcons.pcx", 133, 1, 16, 30);
		ImageTexture goldTexture = Util.LoadTextureFromPCX("Art/city screen/CityIcons.pcx", 67, 1, 21, 30);

		// When non-null, the city whose tile assignments should be shown.

		public City city = null;

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			if (city == null) {
				return;
			}

			// TODO: we probably want to indicate whether a tile is part of the
			// city's big fat cross (assuming a border expand) but ignoring
			// unworked tiles should work for now.
			if (tile.personWorkingTile?.city != city && tile.cityAtTile != city) {
				return;
			}

			int food = tile.foodYield(city.owner);
			int shields = tile.productionYield(city.owner);
			int gold = tile.commerceYield(city.owner);

			int totalWidth = (food * foodTexture.GetWidth()) +
						(shields * shieldTexture.GetWidth()) +
						(gold * goldTexture.GetWidth());
			int currentXOffset = -totalWidth / 2;

			for (int i = 0; i < food; ++i) {
				looseView.DrawTexture(foodTexture, tileCenter + new Vector2(currentXOffset, -15));
				currentXOffset += foodTexture.GetWidth();
			}
			for (int i = 0; i < shields; ++i) {
				looseView.DrawTexture(shieldTexture, tileCenter + new Vector2(currentXOffset, -15));
				currentXOffset += shieldTexture.GetWidth();
			}
			for (int i = 0; i < gold; ++i) {
				looseView.DrawTexture(goldTexture, tileCenter + new Vector2(currentXOffset, -15));
				currentXOffset += goldTexture.GetWidth();
			}
		}

	}
}
