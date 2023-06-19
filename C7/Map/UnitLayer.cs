using System;
using System.Collections.Generic;
using C7GameData;
using C7Engine;
using ConvertCiv3Media;
using Godot;

public partial class UnitLayer : LooseLayer {
	private ImageTexture unitIcons;
	private int unitIconsWidth;
	private ImageTexture unitMovementIndicators;

	// The unit animations, effect animations, and cursor are all drawn as children attached to the looseView but aren't created and attached in
	// any particular order so we must use the ZIndex property to ensure they're properly layered.
	public const int effectAnimZIndex = 150;
	public const int unitAnimZIndex = 100;
	public const int cursorZIndex = 50;

	public UnitLayer() {
		var iconPCX = new Pcx(Util.Civ3MediaPath("Art/Units/units_32.pcx"));
		unitIcons = PCXToGodot.getImageTextureFromPCX(iconPCX);
		unitIconsWidth = (unitIcons.GetWidth() - 1) / 33;

		var moveIndPCX = new Pcx(Util.Civ3MediaPath("Art/interface/MovementLED.pcx"));
		unitMovementIndicators = PCXToGodot.getImageTextureFromPCX(moveIndPCX);
	}

	// Creates a plane mesh facing the positive Z-axis with the given shader attached. The quad is 1.0 units long on both sides,
	// intended to be scaled to the appropriate size when used.
	public static (ShaderMaterial, MeshInstance2D) createShadedQuad(Shader shader) {
		PlaneMesh mesh = new PlaneMesh();
		mesh.SubdivideDepth = 1;
		mesh.Orientation = PlaneMesh.OrientationEnum.Z;
		mesh.Size = new Vector2(1, 1);

		ShaderMaterial shaderMat = new ShaderMaterial();
		shaderMat.Shader = shader;

		MeshInstance2D meshInst = new MeshInstance2D();
		meshInst.Material = shaderMat;
		meshInst.Mesh = mesh;

		return (shaderMat, meshInst);
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

	// AnimationInstance represents an animation appearing on the screen. It's specific to a unit, action, and direction. AnimationInstances have
	// two components: a ShaderMaterial and a MeshInstance2D. The ShaderMaterial runs the unit shader (created by UnitLayer.getShader) with all
	// the parameters set to a particular texture, civ color, direction, etc. The MeshInstance2D is what's actually drawn by Godot, i.e., what's
	// added to the node tree. AnimationInstances are only active for one frame at a time but they live as long as the UnitLayer. They are
	// retrieved or created as needed by getBlankAnimationInstance during the drawing of units and are hidden & requeued for use at the beginning
	// of each frame.

	// should hold animation players instead of animations
	public partial class AnimationInstance {

		public AnimatedSprite2D sprite;
		public AnimatedSprite2D spriteTint;
		public ShaderMaterial material;

		public void SetPosition(Vector2 position) {
			this.sprite.Position = position;
			this.spriteTint.Position = position;
		}

		public int GetNextFrameByProgress(string animation, float progress) {
			// AnimatedSprite2D has a settable FrameProgress field, which I expected to
			// update the current frame of the animation upon setting, but it did not
			// when I tried it, so instead, calculate what the next frame should be
			// based on the progress.
			int frameCount = this.sprite.SpriteFrames.GetFrameCount(animation);
			int nextFrame = (int)((float)frameCount * progress);
			return nextFrame >= frameCount ? frameCount - 1 : (nextFrame < 0 ? 0 : nextFrame);
		}

		public void SetFrame(int frame) {
			this.sprite.Frame = frame;
			this.spriteTint.Frame = frame;
		}

		public void SetAnimation(string name) {
			this.sprite.Animation = name;
			this.spriteTint.Animation = name;
		}

		public void Show() {
			this.sprite.Show();
			this.spriteTint.Show();
		}

		public void Hide() {
			this.sprite.Hide();
			this.spriteTint.Hide();
		}

		public Vector2 FrameSize(string animation) {
			return this.sprite.SpriteFrames.GetFrameTexture(animation, 0).GetSize();
		}

		public AnimationInstance(AnimationManager manager) {
			// AnimationManager manager = looseView.mapView.game.civ3AnimData;

			this.sprite = new AnimatedSprite2D();
			this.sprite.ZIndex = unitAnimZIndex;
			this.sprite.SpriteFrames = manager.spriteFrames;

			this.spriteTint = new AnimatedSprite2D();
			this.spriteTint.ZIndex = unitAnimZIndex;
			this.spriteTint.SpriteFrames = manager.tintFrames;

			this.material = new ShaderMaterial();
			this.material.Shader = GD.Load<Shader>("res://UnitTint.gdshader");
			this.spriteTint.Material = this.material;

			// looseView.AddChild(sprite);
			// looseView.AddChild(spriteTint);
		}
	}

	private List<AnimationInstance> animInsts = new List<AnimationInstance>();
	private int nextBlankAnimInst = 0;

	// Returns the next unused AnimationInstance or creates & returns a new one if none are available.
	public AnimationInstance getBlankAnimationInstance(LooseView looseView) {
		if (nextBlankAnimInst >= animInsts.Count) {
			// animInsts.Add(new AnimationInstance(looseView));
		}
		AnimationInstance inst = animInsts[nextBlankAnimInst];
		nextBlankAnimInst++;
		return inst;
	}

	public void drawUnitAnimFrame(LooseView looseView, MapUnit unit, MapUnit.Appearance appearance, Vector2 tileCenter) {
		AnimationInstance inst = getBlankAnimationInstance(looseView);
		looseView.mapView.game.civ3AnimData.forUnit(unit.unitType, appearance.action).loadSpriteAnimation();
		string animName = AnimationManager.AnimationKey(unit.unitType, appearance.action, appearance.direction);

		// Need to move the sprites upward a bit so that their feet are at the center of the tile. I don't know if spriteHeight/4 is the right
		var animOffset = MapView.cellSize * new Vector2(appearance.offsetX, appearance.offsetY);
		Vector2 position = tileCenter + animOffset - new Vector2(0, inst.FrameSize(animName).Y / 4);
		inst.SetPosition(position);

		var civColor = new Color(unit.owner.color);
		int nextFrame = inst.GetNextFrameByProgress(animName, appearance.progress);
		inst.material.SetShaderParameter("tintColor", new Vector3(civColor.R, civColor.G, civColor.B));

		inst.SetAnimation(animName);
		inst.SetFrame(nextFrame);
		inst.Show();
	}

	public void drawEffectAnimFrame(LooseView looseView, C7Animation anim, float progress, Vector2 tileCenter) {
		// var flicSheet = anim.getFlicSheet();
		// var inst = getBlankAnimationInstance(looseView);
		// setFlicShaderParams(inst.shaderMat, flicSheet, 0, progress);
		// inst.shaderMat.SetShaderParameter("civColor", new Vector3(1, 1, 1));
		// inst.meshInst.Position = tileCenter;
		// inst.meshInst.Scale = new Vector2(flicSheet.spriteWidth, -1 * flicSheet.spriteHeight);
		// inst.meshInst.ZIndex = effectAnimZIndex;
	}

	private AnimatedSprite2D cursorSprite = null;

	public void drawCursor(LooseView looseView, Vector2 position) {
		// Initialize cursor if necessary
		if (cursorSprite == null) {
			cursorSprite = new AnimatedSprite2D();
			SpriteFrames frames = new SpriteFrames();
			cursorSprite.SpriteFrames = frames;
			AnimationManager.loadCursorAnimation("Art/Animations/Cursor/Cursor.flc", ref frames);
			cursorSprite.Animation = "cursor"; // hardcoded in loadCursorAnimation
			looseView.AddChild(cursorSprite);
		}

		const double period = 2.5; // TODO: Just eyeballing this for now. Read the actual period from the INI or something.
		double repCount = (double)Time.GetTicksMsec() / 1000.0 / period;
		float progress = (float)(repCount - Math.Floor(repCount));
		cursorSprite.Position = position;
		int frameCount = cursorSprite.SpriteFrames.GetFrameCount("cursor");
		int nextFrame = (int)((float)frameCount * progress);
		nextFrame = nextFrame >= frameCount ? frameCount - 1 : (nextFrame < 0 ? 0 : nextFrame);
		cursorSprite.Frame = nextFrame;
		cursorSprite.Show();
	}

	public override void onBeginDraw(LooseView looseView, GameData gameData) {
		// Reset animation instances
		for (int n = 0; n < nextBlankAnimInst; n++) {
			animInsts[n].Hide();
		}
		nextBlankAnimInst = 0;

		// Hide cursor if it's been initialized
		cursorSprite?.Hide();

		looseView.mapView.game.updateAnimations(gameData);
	}

	// Returns which unit should be drawn from among a list of units. The list is assumed to be non-empty.
	public MapUnit selectUnitToDisplay(LooseView looseView, List<MapUnit> units) {
		// From the list, pick out which units are (1) the strongest defender vs the currently selected unit, (2) the currently selected unit
		// itself if it's in the list, and (3) any unit that is playing an animation that the player would want to see.
		MapUnit bestDefender = units[0],
			selected = null,
			doingInterestingAnimation = null;
		var currentlySelectedUnit = looseView.mapView.game.CurrentlySelectedUnit;
		foreach (var u in units) {
			if (u == currentlySelectedUnit)
				selected = u;
			if (u.HasPriorityAsDefender(bestDefender, currentlySelectedUnit))
				bestDefender = u;
			if (looseView.mapView.game.animTracker.getUnitAppearance(u).DeservesPlayerAttention())
				doingInterestingAnimation = u;
		}

		// Prefer showing the selected unit, secondly show one doing a relevant animation, otherwise show the top defender
		return selected != null ? selected : (doingInterestingAnimation != null ? doingInterestingAnimation : bestDefender);
	}

	public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
		// First draw animated effects. These will always appear over top of units regardless of draw order due to z-index.
		C7Animation tileEffect = looseView.mapView.game.animTracker.getTileEffect(tile);
		if (tileEffect != null) {
			(_, float progress) = looseView.mapView.game.animTracker.getCurrentActionAndProgress(tile);
			drawEffectAnimFrame(looseView, tileEffect, progress, tileCenter);
		}

		if (tile.unitsOnTile.Count == 0) {
			return;
		}

		var white = Color.Color8(255, 255, 255);

		MapUnit unit = selectUnitToDisplay(looseView, tile.unitsOnTile);
		MapUnit.Appearance appearance = looseView.mapView.game.animTracker.getUnitAppearance(unit);
		Vector2 animOffset = new Vector2(appearance.offsetX, appearance.offsetY) * MapView.cellSize;

		// If the unit we're about to draw is currently selected, draw the cursor first underneath it
		if ((unit != MapUnit.NONE) && (unit == looseView.mapView.game.CurrentlySelectedUnit)) {
			drawCursor(looseView, tileCenter + animOffset);
		}

		drawUnitAnimFrame(looseView, unit, appearance, tileCenter);

		Vector2 indicatorLoc = tileCenter - new Vector2(26, 40) + animOffset;

		int moveIndIndex = (!unit.movementPoints.canMove) ? 4 : ((unit.movementPoints.remaining >= unit.unitType.movement) ? 0 : 2);
		Vector2 moveIndUpperLeft = new Vector2(1 + 7 * moveIndIndex, 1);
		Rect2 moveIndRect = new Rect2(moveIndUpperLeft, new Vector2(6, 6));
		var screenRect = new Rect2(indicatorLoc, new Vector2(6, 6));
		looseView.DrawTextureRectRegion(unitMovementIndicators, screenRect, moveIndRect);

		int hpIndHeight = 6 * (unit.maxHitPoints <= 5 ? unit.maxHitPoints : 5), hpIndWidth = 6;
		Rect2 hpIndBackgroundRect = new Rect2(indicatorLoc + new Vector2(-1, 8), new Vector2(hpIndWidth, hpIndHeight));
		if ((unit.unitType.attack > 0) || (unit.unitType.defense > 0)) {
			float hpFraction = (float)unit.hitPointsRemaining / unit.maxHitPoints;
			looseView.DrawRect(hpIndBackgroundRect, Color.Color8(0, 0, 0));
			float hpHeight = hpFraction * (hpIndHeight - 2);
			if (hpHeight < 1)
				hpHeight = 1;
			var hpContentsRect = new Rect2(hpIndBackgroundRect.Position + new Vector2(1, hpIndHeight - 1 - hpHeight), // position
										   new Vector2(hpIndWidth - 2, hpHeight)); // size
			looseView.DrawRect(hpContentsRect, getHPColor(hpFraction));
			if (unit.isFortified)
				looseView.DrawRect(hpIndBackgroundRect, white, false);
		}

		// Draw lines to show that there are more units on this tile
		if (tile.unitsOnTile.Count > 1) {
			int lineCount = tile.unitsOnTile.Count;
			if (lineCount > 5)
				lineCount = 5;
			for (int n = 0; n < lineCount; n++) {
				var lineStart = indicatorLoc + new Vector2(-2, hpIndHeight + 12 + 4 * n);
				looseView.DrawLine(lineStart, lineStart + new Vector2(8, 0), white);
				looseView.DrawLine(lineStart + new Vector2(0, 1), lineStart + new Vector2(8, 1), Color.Color8(75, 75, 75));
			}
		}
	}
}
