using System;
using System.Collections.Generic;
using C7GameData;
using ConvertCiv3Media;
using Godot;
using Serilog;
using Serilog.Events;

namespace C7.Map {
	public class CityLayer : LooseLayer {

		private ILogger log = LogManager.ForContext<Game>();

		private ImageTexture cityTexture;
		private Dictionary<string, ImageTexture> cityLabels = new Dictionary<string, ImageTexture>();

		private DynamicFont smallFont = new DynamicFont();
		private DynamicFont midSizedFont = new DynamicFont();
		private Pcx cityIcons = Util.LoadPCX("Art/Cities/city icons.pcx");
		private Image nonEmbassyStar;

		const int CITY_LABEL_HEIGHT = 23;
		const int LEFT_RIGHT_BOXES_WIDTH = 24;
		const int LEFT_RIGHT_BOXES_HEIGHT = CITY_LABEL_HEIGHT - 2;
		const int TEXT_ROW_HEIGHT = 9;

		public CityLayer()
		{
			smallFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
			smallFont.Size = 11;

			midSizedFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
			midSizedFont.Size = 18;

			nonEmbassyStar = PCXToGodot.getImageFromPCX(cityIcons, 20, 1, 18, 18);
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
		{
			if (tile.cityAtTile is null) {
				return;
			}

			City city = tile.cityAtTile;

			CityScene cityScene = new CityScene(city, tile, tileCenter);
			looseView.AddChild(cityScene);
		}
	}
}
