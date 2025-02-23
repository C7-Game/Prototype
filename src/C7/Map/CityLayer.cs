using System.Collections.Generic;
using C7GameData;
using Godot;
using Serilog;

namespace C7.Map {
	public class CityLayer : LooseLayer {
		private Dictionary<City, CityScene> citySceneLookup = new();

		public CityLayer() {
		}

		public void UpdateAfterCityDestruction(City city) {
			citySceneLookup.Remove(city, out CityScene cityScene);
			cityScene.Hide();
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			if (tile.cityAtTile is null) {
				return;
			}

			City city = tile.cityAtTile;
			if (!citySceneLookup.ContainsKey(city)) {
				CityScene cityScene = new CityScene(city, tile, new Vector2I((int)tileCenter.X, (int)tileCenter.Y));
				looseView.AddChild(cityScene);
				citySceneLookup[city] = cityScene;
			} else {
				CityScene scene = citySceneLookup[city];
				scene._Draw();
			}
		}
	}
}
