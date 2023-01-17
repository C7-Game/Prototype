using System;
using System.Collections.Generic;
using C7GameData;
using ConvertCiv3Media;
using Godot;
using Serilog;
using Serilog.Events;

namespace C7.Map {
	public class CityLayer : LooseLayer {

		private ILogger log = LogManager.ForContext<CityLayer>();

		private ImageTexture cityTexture;
		private Dictionary<string, ImageTexture> cityLabels = new Dictionary<string, ImageTexture>();

		public CityLayer()
		{

		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
		{
			if (tile.cityAtTile is null) {
				return;
			}

			City city = tile.cityAtTile;

			CityScene cityScene = new CityScene(city, tile, tileCenter);
			looseView.AddChild(cityScene);
			GD.Print("Child count " + looseView.GetChildren().Count);
		}
	}
}
