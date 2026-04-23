using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using Godot;

// The layer responsible for drawing the cursor and tile effects relating to bombardment.
public partial class BombardLayer : LooseLayer {
	private readonly ImageTexture bombardCursorTexture;
	private readonly ImageTexture bombardDenyCursorTexture;
	private readonly ImageTexture bombardTileTexture;

	private TextureRect bombardCursorRect = null;
	private TextureRect bombardDenyCursorRect = null;
	private List<TextureRect> bombardTileRects = [];

	public BombardLayer() {
		bombardCursorTexture = TextureLoader.Load("ui.cursor.bombard");
		bombardDenyCursorTexture = TextureLoader.Load("ui.cursor.bombard_deny");
		bombardTileTexture = TextureLoader.Load("ui.cursor.bombard_tile");
	}

	public void DrawBombardCursor() {
		Input.SetCustomMouseCursor(bombardCursorTexture, hotspot: bombardCursorTexture.Center());
	}
	public void DrawBombardDenyCursor() {
		Input.SetCustomMouseCursor(bombardDenyCursorTexture, hotspot: bombardDenyCursorTexture.Center());
	}

	public void DrawBombardTiles(LooseView looseView, MapUnit unit, Tile tile, Vector2 tileCenter) {
		var bombard = 1; // TODO: unit.unitType.bombard;

		var offsets = new List<(int, int, int)>();
		var indexRange= Enumerable.Range(-bombard, 2*bombard).ToList();
		foreach (var xOffset in indexRange.Select((x, i) => (x, i)))
			foreach (var yOffset in indexRange.Select((y, i) => (y, i)))
				offsets.Add((xOffset.x, yOffset.y, xOffset.i + (2 * bombard * yOffset.i)));

		if (bombardTileRects.Count < indexRange.Count) {
			foreach (var tr in bombardTileRects)
				tr.QueueFree();

			foreach (var _ in offsets) {
				var bombardTileRect = new TextureRect { Texture = bombardTileTexture };
				looseView.AddChild(bombardTileRect);
				bombardTileRects.Add(bombardTileRect);
			}
		}

		foreach (var offset in offsets) {
			var bombardTileRect = bombardTileRects[offset.Item3];
			bombardTileRect?.Hide();

			var textureOffset = new Vector2(offset.Item1 * bombardTileTexture.GetWidth(),
				offset.Item2 * bombardTileTexture.GetHeight());

			bombardTileRect.Position = tileCenter + textureOffset;
			bombardTileRect.Show();
		}
	}

	public override void onBeginDraw(LooseView looseView, GameData gameData) {
		// clear previous draw
		bombardCursorRect?.Hide();
		bombardDenyCursorRect?.Hide();
		bombardTileRects.ForEach(x => x?.Hide());
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
		var bombardInfo = looseView.mapView.game.bombardInfo;

		if (bombardInfo == null || bombardInfo.bombardingUnit.location != tile)
			return;

		var unit = bombardInfo.bombardingUnit;

		if (bombardInfo.mouseTile != null) {
			var mouseTileDiff = bombardInfo.mouseTile.rankDistanceTo(tile);
			if (mouseTileDiff <= 3)
				DrawBombardCursor();
			else
				DrawBombardDenyCursor();
		}

		DrawBombardTiles(looseView, unit, tile, tileCenter);

		// if (gotoInfo.path != null && unit.CanEnterTile(gotoInfo.destinationTile, TileProbe.DeclareWarProbe())) {
		// 	List<Tile> tiles = new List<Tile>();
		// 	tiles.Add(unitOriginTile);
		// 	tiles.AddRange(gotoInfo.path.path);
		//
		// 	for (int i = 0; i < tiles.Count - 1; i++) {
		// 		Tile currentTile = tiles[i];
		// 		Tile nextTile = tiles[i + 1];
		//
		// 		// Variable width of the line to account for various camera zoom levels.
		// 		// The end result should look pretty much the same to the player on any zoom level.
		// 		float lineWidth = Math.Max(1f / looseView.mapView.cameraZoom, 1f);
		//
		// 		// We draw only the lines between tiles that are in our visible area
		// 		// with one or two tile buffer on both axis. How many is determined in the MapView
		// 		// by the getVisibleRegion().
		// 		// This is not only saving draw calls which is great, but there is a bigger reason.
		// 		// Imagine just loading the game, not moving the camera at all
		// 		// and press G to instruct a unit to move somewhere.
		// 		// The path is precomputed by another module, so we know the route to our destination.
		// 		// But if the path goes outside the visible area + the buffer, there is an issue.
		// 		// The visible region is only what you see at the screen plus the tiny buffer.
		// 		// These are the only tiles the player has seen and calculated the centers of, so far.
		// 		// If we try to draw lines between tiles that are outside this visible region, we will
		// 		// get an error because we don't know yet what these centers are. We either have to move the camera
		// 		// and calculate them, or precompute a huge buffer of tiles which is not practical at all.
		// 		if (looseView.tileCenters.TryGetValue(currentTile, out Vector2 currentTileCenter)
		// 			&& looseView.tileCenters.TryGetValue(nextTile, out Vector2 nextTileCenter)) {
		// 			bombardCursorRect?.Hide();
		// 			looseView.DrawLine(currentTileCenter, nextTileCenter, Colors.Red, width: lineWidth);
		// 			DrawStaticCursor(looseView, nextTileCenter, gotoInfo.moveCost, gotoInfo.attackingMove);
		// 		}
		// 	}
		// }
	}
}
