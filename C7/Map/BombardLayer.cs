using System.Linq;
using C7GameData;
using Godot;

// The layer responsible for drawing the cursor and tile effects relating to bombardment.
public partial class BombardLayer : LooseLayer {
	private readonly ImageTexture bombardCursorTexture;
	private readonly ImageTexture bombardDenyCursorTexture;

	private TextureRect bombardCursorRect = null;
	private TextureRect bombardDenyCursorRect = null;

	private Color bombardRed = Color.Color8(200, 0, 0, 225);
	private float bombardGridLineWidth = (float)1.0;

	public BombardLayer() {
		bombardCursorTexture = TextureLoader.Load("ui.cursor.bombard");
		bombardDenyCursorTexture = TextureLoader.Load("ui.cursor.bombard_deny");
	}

	public void DrawBombardCursor() {
		Input.SetCustomMouseCursor(bombardCursorTexture, hotspot: bombardCursorTexture.Center());
	}
	public void DrawBombardDenyCursor() {
		Input.SetCustomMouseCursor(bombardDenyCursorTexture, hotspot: bombardDenyCursorTexture.Center());
	}

	public override void onBeginDraw(LooseView looseView, GameData gameData) {
		bombardCursorRect?.Hide();
		bombardDenyCursorRect?.Hide();
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
		var bombardInfo = looseView.mapView.game.bombardInfo;
		if (bombardInfo == null || bombardInfo.bombardingUnit.location != tile)
			return;

		var unit = bombardInfo.bombardingUnit;
		var range = unit.unitType.bombardRange;
		var reachableTiles = tile.GetTilesWithinRankDistance(range);
		var bombardTiles = reachableTiles.Except([tile]).ToHashSet();

		// Choose one of two cursors depending on mouse tile hover
		if (bombardInfo.mouseTile != null) {
			var bombardable = bombardTiles.Contains(bombardInfo.mouseTile);
			if (bombardable)
				DrawBombardCursor();
			else
				DrawBombardDenyCursor();
		}

		// Draw bombard grid
		foreach (var bt in bombardTiles) {
			var center = MapView.cellSize * new Vector2(bt.XCoordinate + 1, bt.YCoordinate + 1);
			drawBombardTile(looseView, center);
		}
	}

	private void drawBombardTile(LooseView looseView, Vector2 tileCenter) {
		var cS = MapView.cellSize;
		var left = tileCenter + new Vector2(-cS.X, 0);
		var top = tileCenter + new Vector2(0, -cS.Y);
		var right = tileCenter + new Vector2(cS.X, 0);
		var bottom = tileCenter + new Vector2(0, cS.Y);
		looseView.DrawLine(left, top, bombardRed, bombardGridLineWidth);
		looseView.DrawLine(top, right, bombardRed, bombardGridLineWidth);
		looseView.DrawLine(left, bottom, bombardRed, bombardGridLineWidth);
		looseView.DrawLine(bottom, right, bombardRed, bombardGridLineWidth);
	}
}
