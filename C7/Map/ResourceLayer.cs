using C7GameData;
using Godot;
using Resource = C7GameData.Resource;
using Serilog;

namespace C7.Map
{
	public partial class ResourceLayer : LooseLayer
	{
		private ILogger log = LogManager.ForContext<ResourceLayer>();

		private static readonly Vector2 resourceSize = new Vector2(50, 50);
		private int maxRow;
		private ImageTexture resourceTexture;

		public ResourceLayer()
		{
			resourceTexture = Util.LoadTextureFromPCX("Art/resources.pcx");
			maxRow = (resourceTexture.GetHeight() / 50) - 1;
		}
		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter)
		{
			Resource resource = tile.Resource;
			if (resource != Resource.NONE) {
				int resourceIcon = tile.Resource.Icon;
				int row = resourceIcon / 6;
				int col = resourceIcon % 6;
				if (row > maxRow) {
					log.Warning("Resource icon for " + resource.Name + " is too high");
					return;
				}
				Rect2 resourceRectangle = new Rect2(col * resourceSize.X, row * resourceSize.Y, resourceSize);
				Rect2 screenTarget = new Rect2(tileCenter - 0.5f * resourceSize, resourceSize);
				looseView.DrawTextureRectRegion(resourceTexture, screenTarget, resourceRectangle);
			}
		}
	}
}
