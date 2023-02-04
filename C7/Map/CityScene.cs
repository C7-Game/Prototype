using System;
using C7GameData;
using ConvertCiv3Media;
using Godot;
using Serilog;
using Serilog.Events;

namespace C7.Map {
	public class CityScene : Node2D {
		private ILogger log = LogManager.ForContext<CityScene>();

		private readonly Vector2 citySpriteSize;

		private ImageTexture cityTexture;
		private TextureRect cityGraphics = new TextureRect();
		private CityLabelScene cityLabelScene;

		public CityScene(City city, Tile tile, Vector2 tileCenter) {
			cityLabelScene = new CityLabelScene(city, tile, tileCenter);

			//TODO: Generalize, support multiple city types, etc.
			Pcx pcx = Util.LoadPCX("Art/Cities/rMIDEAST.PCX");
			int height = pcx.Height/4;
			int width = pcx.Width/3;
			cityTexture = Util.LoadTextureFromPCX("Art/Cities/rMIDEAST.PCX", 0, 0, width, height);
			citySpriteSize = new Vector2(width, height);

			cityGraphics.MarginLeft = tileCenter.x - (float)0.5 * citySpriteSize.x;
			cityGraphics.MarginTop = tileCenter.y - (float)0.5 * citySpriteSize.y;
			cityGraphics.MouseFilter = Control.MouseFilterEnum.Ignore;
			cityGraphics.Texture = cityTexture;

			AddChild(cityGraphics);
			AddChild(cityLabelScene);
		}

		public override void _Draw() {
			base._Draw();
			cityLabelScene._Draw();
		}
	}
}
