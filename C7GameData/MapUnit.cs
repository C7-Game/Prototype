namespace C7GameData
{

using System;
using System.Collections.Generic;

/**
 * A unit on the map.  Not to be confused with a unit prototype.
 **/
public class MapUnit
{
	public string guid  {get;}
	public UnitPrototype unitType {get; set;}
	public Player owner {get; set;}
	public Tile location {get; set;}

	public int movementPointsRemaining {get; set;}
	public int hitPointsRemaining {get; set;}
	public int maxHitPoints {
		get {
			return 3; // Eventually we'll add HP from experience and the type's inherent bonus
		}
	}
	public bool isFortified {get; set;}
	//sentry, etc. will come later.  For now, let's just have a couple things so we can cycle through units that aren't fortified.

	public TileDirection facingDirection;

	//This probably should not be serialized.  In .NET, we'd add the [ScriptIgnore] and using System.Web.Script.Serialization.
	//But .NET Core doesn't support that.  So, we'll have to figure out something else.  Maybe a library somewhere.
	public List<string> availableActions = new List<string>();

	public MapUnit()
	{
		guid = Guid.NewGuid().ToString();
	}

	public override string ToString()
	{
		if (this != MapUnit.NONE) {
			return unitType.name + " with " + movementPointsRemaining + " movement points and " + hitPointsRemaining + " hit points, guid = " + guid;
		}
		else {
			return "This is the NONE unit";
		}
	}

	// TODO: The contents of this enum are copy-pasted from UnitAction in Civ3UnitSprite.cs. We should unify these so we don't have two different
	// but virtually identical enums.
	public enum AnimatedAction {
		BLANK,
		DEFAULT,
		WALK,
		RUN,
		ATTACK1,
		ATTACK2,
		ATTACK3,
		DEFEND,
		DEATH,
		DEAD,
		FORTIFY,
		FORTIFYHOLD,
		FIDGET,
		VICTORY,
		TURNLEFT,
		TURNRIGHT,
		BUILD,
		ROAD,
		MINE,
		IRRIGATE,
		FORTRESS,
		CAPTURE,
		JUNGLE,
		FOREST,
		PLANT
	}

	public struct ActiveAnimation {
		public AnimatedAction action;
		public TileDirection direction;
		public float progress; // Varies 0 to 1
		public float offsetX, offsetY; // Offset is in grid cells from the unit's location

		// When true, indicates that the animation is still playing (f.e. a unit is still running between tiles) so the UI shouldn't yet
		// autoselect another unit.
		public bool keepUnitSelected()
		{
			// TODO: Special rules for different animations. We don't need to see workers do their thing but we do want to watch units
			// move. IMO we should also not show units fortifying even though I know the original game does.
			return progress < 1.0;
		}
	}

	public ulong animStartTimeMS;
	public AnimatedAction animAction = AnimatedAction.DEFAULT;

	public ActiveAnimation getActiveAnimation(ulong currentTimeMS)
	{
		double runningTimeS = (currentTimeMS - animStartTimeMS) / 1000.0;
		double animDuration = 0.5; // TODO: Read this from the INI files somehow
		float progress = (float)(runningTimeS / animDuration);

		var animAction = this.animAction;

		// Replace run animation with default after the running is finished
		if ((animAction == AnimatedAction.RUN) && (progress >= 1f))
			animAction = AnimatedAction.DEFAULT;

		float offsetX = 0, offsetY = 0;
		if ((animAction == AnimatedAction.RUN) && (progress < 1f)) {
			(int dX, int dY) = facingDirection.toCoordDiff();
			offsetX = -1 * dX * (1f - progress);
			offsetY = -1 * dY * (1f - progress);
		}

		return new ActiveAnimation { action = animAction, direction = facingDirection, progress = progress, offsetX = offsetX, offsetY = offsetY };
	}

	public static MapUnit NONE = new MapUnit();
}

}
