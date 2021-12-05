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

	public struct ActiveAnimation {
		public string name; // Flic file name. TODO: Maybe this should be an enum of animation types?
		public TileDirection direction;
		public float progress; // Varies 0 to 1
		public float offsetX, offsetY; // Offset is in grid cells from the unit's location
	}

	// public ActiveAnimation activeAnim;
	public ulong animStartTimeMS;

	// TODO: This needs to be part of the engine eventually.
	public ActiveAnimation getActiveAnimation(ulong currentTimeMS)
	{
		double runningTimeS = (currentTimeMS - animStartTimeMS) / 1000.0;
		double animDuration = 0.5; // TODO: Read this from the INI files somehow
		float progress = (float)(runningTimeS / animDuration);

		string animName;
		if ((! isFortified) || (unitType.name == "Worker"))
			animName = String.Format("Art/Units/{0}/{0}Default.flc", unitType.name);
		else
			animName = String.Format("Art/Units/{0}/{0}Fortify.flc", unitType.name);

		return new ActiveAnimation { name = animName, direction = facingDirection, progress = progress, offsetX = 0, offsetY = 0 };
	}

	public static MapUnit NONE = new MapUnit();
}

}
