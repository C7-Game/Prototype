using C7GameData;
using Godot;
using Resource = C7GameData.Resource;

namespace C7.Map
{
	public class ResourceLayer : LooseLayer
	{
		private static readonly Vector2 resourceSize = new Vector2(50, 50);
		private int maxRow;
		private ImageTexture resourceTexture;
		private bool debugMessage = false;

		public ResourceLayer()
		{
			resourceTexture = Util.LoadTextureFromPCX("Art/resources.pcx");
			maxRow = (resourceTexture.GetHeight() / 50) - 1;
		}
		public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
		{
			Resource resource = tile.Resource;
			if (resource != Resource.NONE) {
				int resourceIcon = tile.Resource.Icon;
				int row = resourceIcon / 6;
				int col = resourceIcon % 6;
				if (row > maxRow) {
					GD.Print("Resource icon for " + resource.Name + " is too high");
					return;
				}
				Rect2 resourceRectangle = new Rect2(col * resourceSize.x, row * resourceSize.y, resourceSize);
				Rect2 screenTarget = new Rect2(tileCenter - 0.5f * resourceSize, resourceSize);
				looseView.DrawTextureRectRegion(resourceTexture, screenTarget, resourceRectangle);
			}
		}
	}
}
