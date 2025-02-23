using System;
using System.Collections.Generic;
using C7GameData;
using Godot;

namespace C7.Map {
	public partial class TileAssignmentLayer : LooseLayer {
		ImageTexture foodTexture = Util.LoadTextureFromPCX("Art/city screen/CityIcons.pcx", 195, 1, 21, 30);
		ImageTexture shieldTexture = Util.LoadTextureFromPCX("Art/city screen/CityIcons.pcx", 133, 1, 16, 30);
		ImageTexture goldTexture = Util.LoadTextureFromPCX("Art/city screen/CityIcons.pcx", 67, 1, 21, 30);

		private const int tileWidth = 128;
		private const int tileHeight = 64;

		// When non-null, the city whose tile assignments should be shown.

		public City city = null;
		private HashSet<Tile> workableTiles;

		public override void onBeginDraw(LooseView looseView, GameData gameData) {
			if (city == null) {
				return;
			}
			workableTiles = city.GetWorkableTiles();

			// Include the city center in the "workable" tiles to avoid having
			// a border drawn there.
			workableTiles.Add(city.location);
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			if (city == null) {
				return;
			}

			// Draw a nice border around our city's workable tiles.
			//
			// TODO: use the fog of war layer to highlight the BFC.
			if (workableTiles.Contains(tile)) {
				DrawWorkableTileBorder(looseView, tile, tileCenter);

				// If one of our workable tiles is worked by another city, draw
				// a rectangle around it.
				if (tile.personWorkingTile != null && tile.personWorkingTile.city != city) {
					DrawOccupiedTileSquare(looseView, tileCenter);
				}
			}

			// Only draw yields for our citizens.
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

		private void DrawWorkableTileBorder(LooseView looseView, Tile tile, Vector2 tileCenter) {
			if (!workableTiles.Contains(tile.neighbors[TileDirection.NORTHWEST])) {
				looseView.DrawLine(tileCenter + new Vector2(-tileWidth / 2, 0),
									tileCenter + new Vector2(0, -tileHeight / 2),
									Colors.White, /*width=*/2);
			}

			if (!workableTiles.Contains(tile.neighbors[TileDirection.NORTHEAST])) {
				looseView.DrawLine(tileCenter + new Vector2(tileWidth / 2, 0),
									tileCenter + new Vector2(0, -tileHeight / 2),
									Colors.White, /*width=*/2);
			}

			if (!workableTiles.Contains(tile.neighbors[TileDirection.SOUTHWEST])) {
				looseView.DrawLine(tileCenter + new Vector2(-tileWidth / 2, 0),
									tileCenter + new Vector2(0, tileHeight / 2),
									Colors.White, /*width=*/2);
			}

			if (!workableTiles.Contains(tile.neighbors[TileDirection.SOUTHEAST])) {
				looseView.DrawLine(tileCenter + new Vector2(tileWidth / 2, 0),
									tileCenter + new Vector2(0, tileHeight / 2),
									Colors.White, /*width=*/2);
			}
		}

		private void DrawOccupiedTileSquare(LooseView looseView, Vector2 tileCenter) {
			// Thick brown outline.
			{
				int lineWidth = 4;
				int width = tileWidth - lineWidth * 4;
				int height = tileHeight - lineWidth * 2;

				Color brown = new Color(166.0f/256, 116.0f/256, 87.0f/256);
				DrawSquare(looseView, tileCenter, brown, lineWidth, width, height);
			}

			// Black borders of the brown outline.
			{
				int lineWidth = 1;
				int width = tileWidth - 4;
				int height = tileHeight - 2;

				DrawSquare(looseView, tileCenter, Colors.Black, lineWidth, width, height);
			}
			{
				int lineWidth = 1;
				int width = tileWidth - 20;
				int height = tileHeight - 10;

				DrawSquare(looseView, tileCenter, Colors.Black, lineWidth, width, height);
			}
		}

		private void DrawSquare(LooseView looseView, Vector2 tileCenter, Color color, int lineWidth, int tileWidth, int tileHeight) {
			looseView.DrawLine(tileCenter + new Vector2(-tileWidth / 2, 0),
								tileCenter + new Vector2(0, -tileHeight / 2),
								color, lineWidth);
			looseView.DrawLine(tileCenter + new Vector2(tileWidth / 2, 0),
								tileCenter + new Vector2(0, -tileHeight / 2),
								color, lineWidth);
			looseView.DrawLine(tileCenter + new Vector2(-tileWidth / 2, 0),
								tileCenter + new Vector2(0, tileHeight / 2),
								color, lineWidth);
			looseView.DrawLine(tileCenter + new Vector2(tileWidth / 2, 0),
								tileCenter + new Vector2(0, tileHeight / 2),
								color, lineWidth);
		}
	}
}
