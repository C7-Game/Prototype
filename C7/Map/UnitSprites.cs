using System;
using C7GameData;
using ConvertCiv3Media;
using Godot;
using C7.Map;

// UnitSprite represents an animated unit. It's specific to a unit, action, and direction.
// UnitSprite comprises two sprites: a base sprite and a civ color-tinted sprite. The
// shading is done in the UnitTint.gdshader shader.
// TODO: once https://github.com/godotengine/godot/issues/62943 is solved, UnitSprite should
//       use a single instance of a material and UnitSprite use a per instance uniform
public partial class UnitSprite : Node2D {

	private readonly int unitAnimZIndex = MapZIndex.Units;
	private readonly string unitShaderPath = "res://UnitTint.gdshader";
	private readonly string unitColorShaderParameter = "tintColor";
	private Shader unitShader;

	public AnimatedSprite2D sprite;
	public AnimatedSprite2D spriteTint;
	public ShaderMaterial material;

	public int GetNextFrameByProgress(string animation, float progress) {
		if (!sprite.SpriteFrames.HasAnimation(animation)) {
			throw new ArgumentException($"no such animation: {animation}");
		}
		int frameCount = sprite.SpriteFrames.GetFrameCount(animation);
		int nextFrame = (int)((float)frameCount * progress);
		return Mathf.Clamp(nextFrame, 0, frameCount - 1);
	}

	public void SetFrame(int frame) {
		sprite.Frame = frame;
		spriteTint.Frame = frame;
	}

	public void SetAnimation(string name) {
		sprite.Animation = name;
		spriteTint.Animation = name;
	}

	public void Play(string name) {
		sprite.Play(name);
		spriteTint.Play(name);
	}

	public void SetColor(Color color) {
		material.SetShaderParameter(unitColorShaderParameter, new Vector3(color.R, color.G, color.B));
	}

	public Vector2 FrameSize(string animation) {
		return sprite.SpriteFrames.GetFrameTexture(animation, 0).GetSize();
	}

	public UnitSprite(AnimationManager manager) {
		ZIndex = unitAnimZIndex;
		sprite = new AnimatedSprite2D{
			SpriteFrames = manager.spriteFrames,
		};
		spriteTint = new AnimatedSprite2D{
			SpriteFrames= manager.tintFrames,
		};

		material = new ShaderMaterial();
		unitShader = GD.Load<Shader>(unitShaderPath);
		material.Shader = unitShader;
		spriteTint.Material = material;

		AddChild(sprite);
		AddChild(spriteTint);
	}
}

public partial class CursorSprite : Node2D {
	private readonly string animationPath = "Art/Animations/Cursor/Cursor.flc";
	private readonly string animationName = "cursor";
	private readonly double period = 2.5;
	private readonly int cursorAnimZIndex = MapZIndex.Cursor;
	private AnimatedSprite2D sprite;
	private int frameCount;

	public CursorSprite() {
		ZIndex = cursorAnimZIndex;
		SpriteFrames frames = new SpriteFrames();
		AnimationManager.loadCursorAnimation(animationPath, animationName, ref frames);
		sprite = new AnimatedSprite2D{
			SpriteFrames = frames,
			Animation = animationName,
		};
		frameCount = sprite.SpriteFrames.GetFrameCount(animationName);
		AddChild(sprite);
	}

	public override void _Process(double delta) {
		double repCount = (double)Time.GetTicksMsec() / 1000.0 / period;
		float progress = (float)(repCount - Math.Floor(repCount));
		int nextFrame = (int)((float)frameCount * progress);
		nextFrame = Mathf.Clamp(nextFrame, 0, frameCount - 1);
		sprite.Frame = nextFrame;
		base._Process(delta);
	}
}

public partial class UnitLayer {
	private ImageTexture unitIcons;
	private int unitIconsWidth;
	private ImageTexture unitMovementIndicators;

	public UnitLayer() {
		var iconPCX = new Pcx(Util.Civ3MediaPath("Art/Units/units_32.pcx"));
		unitIcons = PCXToGodot.getImageTextureFromPCX(iconPCX);
		unitIconsWidth = (unitIcons.GetWidth() - 1) / 33;

		var moveIndPCX = new Pcx(Util.Civ3MediaPath("Art/interface/MovementLED.pcx"));
		unitMovementIndicators = PCXToGodot.getImageTextureFromPCX(moveIndPCX);
	}

	public Color getHPColor(float fractionRemaining) {
		if (fractionRemaining >= 0.67f) {
			return Color.Color8(0, 255, 0);
		} else if (fractionRemaining >= 0.34f) {
			return Color.Color8(255, 255, 0);
		} else {
			return Color.Color8(255, 0, 0);
		}
	}

	public void drawEffectAnimFrame(C7Animation anim, float progress, Vector2 tileCenter) {
		// var flicSheet = anim.getFlicSheet();
		// var inst = getBlankAnimationInstance(looseView);
		// setFlicShaderParams(inst.shaderMat, flicSheet, 0, progress);
		// inst.shaderMat.SetShaderParameter("civColor", new Vector3(1, 1, 1));
		// inst.meshInst.Position = tileCenter;
		// inst.meshInst.Scale = new Vector2(flicSheet.spriteWidth, -1 * flicSheet.spriteHeight);
		// inst.meshInst.ZIndex = effectAnimZIndex;
	}

	public void drawObject(GameData gameData, Tile tile, Vector2 tileCenter) {
		// First draw animated effects. These will always appear over top of units regardless of draw order due to z-index.
		// C7Animation tileEffect = looseView.mapView.game.animTracker.getTileEffect(tile);
		// if (tileEffect != null) {
		// 	(_, float progress) = looseView.mapView.game.animTracker.getCurrentActionAndProgress(tile);
		// 	drawEffectAnimFrame(looseView, tileEffect, progress, tileCenter);
		// }

		if (tile.unitsOnTile.Count == 0) {
			return;
		}

		var white = Color.Color8(255, 255, 255);

		// MapUnit unit = selectUnitToDisplay(looseView, tile.unitsOnTile);
		// MapUnit.Appearance appearance = looseView.mapView.game.animTracker.getUnitAppearance(unit);
		// Vector2 animOffset = new Vector2(appearance.offsetX, appearance.offsetY) * OldMapView.cellSize;

		// // If the unit we're about to draw is currently selected, draw the cursor first underneath it
		// if ((unit != MapUnit.NONE) && (unit == looseView.mapView.game.CurrentlySelectedUnit)) {
		// 	drawCursor(looseView, tileCenter + animOffset);
		// }

		// drawUnitAnimFrame(looseView, unit, appearance, tileCenter);

		// Vector2 indicatorLoc = tileCenter - new Vector2(26, 40) + animOffset;

		// int moveIndIndex = (!unit.movementPoints.canMove) ? 4 : ((unit.movementPoints.remaining >= unit.unitType.movement) ? 0 : 2);
		// Vector2 moveIndUpperLeft = new Vector2(1 + 7 * moveIndIndex, 1);
		// Rect2 moveIndRect = new Rect2(moveIndUpperLeft, new Vector2(6, 6));
		// var screenRect = new Rect2(indicatorLoc, new Vector2(6, 6));
		// looseView.DrawTextureRectRegion(unitMovementIndicators, screenRect, moveIndRect);

		// int hpIndHeight = 6 * (unit.maxHitPoints <= 5 ? unit.maxHitPoints : 5), hpIndWidth = 6;
		// Rect2 hpIndBackgroundRect = new Rect2(indicatorLoc + new Vector2(-1, 8), new Vector2(hpIndWidth, hpIndHeight));
		// if ((unit.unitType.attack > 0) || (unit.unitType.defense > 0)) {
		// 	float hpFraction = (float)unit.hitPointsRemaining / unit.maxHitPoints;
		// 	looseView.DrawRect(hpIndBackgroundRect, Color.Color8(0, 0, 0));
		// 	float hpHeight = hpFraction * (hpIndHeight - 2);
		// 	if (hpHeight < 1)
		// 		hpHeight = 1;
		// 	var hpContentsRect = new Rect2(hpIndBackgroundRect.Position + new Vector2(1, hpIndHeight - 1 - hpHeight), // position
		// 								   new Vector2(hpIndWidth - 2, hpHeight)); // size
		// 	looseView.DrawRect(hpContentsRect, getHPColor(hpFraction));
		// 	if (unit.isFortified)
		// 		looseView.DrawRect(hpIndBackgroundRect, white, false);
		// }

		// // Draw lines to show that there are more units on this tile
		// if (tile.unitsOnTile.Count > 1) {
		// 	int lineCount = tile.unitsOnTile.Count;
		// 	if (lineCount > 5)
		// 		lineCount = 5;
		// 	for (int n = 0; n < lineCount; n++) {
		// 		var lineStart = indicatorLoc + new Vector2(-2, hpIndHeight + 12 + 4 * n);
		// 		looseView.DrawLine(lineStart, lineStart + new Vector2(8, 0), white);
		// 		looseView.DrawLine(lineStart + new Vector2(0, 1), lineStart + new Vector2(8, 1), Color.Color8(75, 75, 75));
		// 	}
		// }
	}
}
