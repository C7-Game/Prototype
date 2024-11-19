using C7GameData;
using ConvertCiv3Media;
using Godot;
using Serilog;

namespace C7.Map {
	public partial class CityScene : Sprite2D {
		private ILogger log = LogManager.ForContext<CityScene>();

		private readonly Vector2 citySpriteSize;

		private ImageTexture cityTexture;
		private CityLabelScene cityLabelScene;

		public CityScene(City city, Tile tile) {
			ZIndex = MapZIndex.Cities;
			cityLabelScene = new CityLabelScene(city, tile);

			//TODO: Generalize, support multiple city types, etc.
			Pcx pcx = Util.LoadPCX("Art/Cities/rMIDEAST.PCX");
			int height = pcx.Height/4;
			int width = pcx.Width/3;
			cityTexture = Util.LoadTextureFromPCX("Art/Cities/rMIDEAST.PCX", 0, 0, width, height, false);
			citySpriteSize = new Vector2(width, height);

			Texture = cityTexture;

			AddChild(cityLabelScene);
		}
	}
}
