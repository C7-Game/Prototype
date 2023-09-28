using System.Collections.Generic;
using C7GameData;
using Godot;
using Serilog;

namespace C7.Map {
	public class CityLayer : LooseLayer {

		private ILogger log = LogManager.ForContext<CityLayer>();

		private ImageTexture cityTexture;
		private Dictionary<string, ImageTexture> cityLabels = new Dictionary<string, ImageTexture>();

		private List<City> citiesWithScenes = new List<City>();
		private Dictionary<City, CityScene> citySceneLookup = new Dictionary<City, CityScene>();

		public CityLayer()
		{
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
		{
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
