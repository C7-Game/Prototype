using System;
using System.Collections.Generic;
using C7GameData;
using C7Engine;
using ConvertCiv3Media;
using Godot;

// The layer responsible for drawing the cursor when the player is selecting a
// move via the "goto" command.
public partial class GotoLayer : LooseLayer {
	public GotoLayer() {
		// Load the font we'll use for the goto move counter.
		//
		// We skip the cache so that we can change the size without affecting other
		// code using the same font.
		//
		// We use FixedSize so Godot can calculate the width of the text for centering.
		gotoLabelFont = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf", null, ResourceLoader.CacheMode.Ignore);
		gotoLabelFont.FixedSize = 25;

		gotoLabelFontTheme.DefaultFont = gotoLabelFont;
		gotoLabelFontTheme.SetColor("font_color", "Label", Color.Color8(255, 255, 255));
		gotoLabelFontTheme.SetFontSize("font_size", "Label", 20);
	}

	// The GoTo cursor and label fields.
	private AnimatedSprite2D gotoCursorSprite = null;
	private Label gotoLabel = null;
	Theme gotoLabelFontTheme = new();
	FontFile gotoLabelFont = new();

	public void drawGotoCursor(LooseView looseView, Vector2 position, int moves) {
		// Initialize cursor if necessary
		if (gotoCursorSprite == null) {
			gotoCursorSprite = new AnimatedSprite2D();
			SpriteFrames frames = new SpriteFrames();
			gotoCursorSprite.SpriteFrames = frames;
			AnimationManager.loadCursorAnimation("Art/Animations/Cursor/Cursor.flc", ref frames);
			gotoCursorSprite.Animation = "cursor"; // hardcoded in loadCursorAnimation

			gotoLabel = new() {
				Theme = gotoLabelFontTheme
			};

			looseView.AddChild(gotoLabel);
			looseView.AddChild(gotoCursorSprite);
		}

		gotoLabel.Text = moves.ToString();
		Vector2 labelSize = gotoLabelFont.GetStringSize(gotoLabel.Text);
		gotoLabel.Position = position - labelSize / 2;

		// This logic is copied from UnitLayer.cs
		const double period = 2.5;
		double repCount = (double)Time.GetTicksMsec() / 1000.0 / period;
		float progress = (float)(repCount - Math.Floor(repCount));
		gotoCursorSprite.Position = position;
		int frameCount = gotoCursorSprite.SpriteFrames.GetFrameCount("cursor");
		int nextFrame = (int)((float)frameCount * progress);
		nextFrame = nextFrame >= frameCount ? frameCount - 1 : (nextFrame < 0 ? 0 : nextFrame);
		gotoCursorSprite.Frame = nextFrame;

		// Now that we're positioned our objects, we can display them again.
		gotoCursorSprite.Show();
		gotoLabel.Show();
	}

	public override void onBeginDraw(LooseView looseView, GameData gameData) {
		// Hide the cursor if it has been initialized
		gotoCursorSprite?.Hide();
		gotoLabel?.Hide();

		looseView.mapView.game.updateAnimations(gameData);
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
		// We set the move cost to -1 in Game.cs if the move is invalid for some reason.
		if (looseView.mapView.game.gotoInfo?.destinationTile == tile && looseView.mapView.game.gotoInfo.moveCost >= 0) {
			drawGotoCursor(looseView, tileCenter, looseView.mapView.game.gotoInfo.moveCost);
		} else if (looseView.mapView.game.gotoInfo?.pathCoords
				?.Contains(new System.Numerics.Vector2(tile.XCoordinate, tile.YCoordinate)) == true) {
			// If this tile is part of the path, draw a little dot to represent that.
			looseView.DrawCircle(tileCenter, 5, Color.Color8(255, 255, 255));
		}
	}
}
