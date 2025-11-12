using C7GameData;
using Godot;
using Resource = C7GameData.Resource;
using Serilog;

namespace C7.Map {
	public partial class ResourceLayer : LooseLayer {
		private ILogger log = LogManager.ForContext<ResourceLayer>();

		private static readonly Vector2 resourceSize = new Vector2(50, 50);

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			Resource resource = tile.Resource;
			if (resource == Resource.NONE) {
				return;
			}

			if (!ResourceVisible(gameData, tile)) {
				return;
			}

			if (!looseView.mapView.game.Global.ModernGraphicsActive) {
				ImageTexture shadows = TextureLoader.Load("resources.shadows", resource, useCache: true);
				looseView.DrawTexture(shadows, tileCenter - 0.5f * shadows.GetSize());
			}

			ImageTexture texture = TextureLoader.Load("resources.large", resource, useCache: true);
			looseView.DrawTexture(texture, tileCenter - 0.5f * texture.GetSize());
		}

		private bool ResourceVisible(GameData gameData, Tile t) {
			if (gameData.observerMode) {
				return true;
			}
			return gameData.GetFirstHumanPlayer().KnowsAboutResource(t.Resource);
		}
	}
}
