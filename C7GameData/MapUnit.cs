using C7GameData.AIData;

namespace C7GameData
{

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/**
 * A unit on the map.  Not to be confused with a unit prototype.
 **/
public class MapUnit
{
	public string guid  {get;}
	public UnitPrototype unitType {get; set;}
	public Player owner {get; set;}
	public Tile location {get; set;}
	public TilePath path {get; set;}

	public string experienceLevelKey;
	[JsonIgnore]
	public ExperienceLevel experienceLevel {get; set;}

	public int movementPointsRemaining {get; set;}
	public int hitPointsRemaining {get; set;}
	public int maxHitPoints {
		get {
			return experienceLevel.baseHitPoints; // TODO: Include bonus HP from unit type
		}
	}
	public bool isFortified {get; set;}
	//sentry, etc. will come later.  For now, let's just have a couple things so we can cycle through units that aren't fortified.

	public TileDirection facingDirection;

	//This probably should not be serialized.  In .NET, we'd add the [ScriptIgnore] and using System.Web.Script.Serialization.
	//But .NET Core doesn't support that.  So, we'll have to figure out something else.  Maybe a library somewhere.
	public List<string> availableActions = new List<string>();
	public UnitAIData currentAIData;

	public MapUnit()
	{
		guid = Guid.NewGuid().ToString();
	}

	public bool IsBusy() {
		return isFortified || (path != null && path.PathLength() > 0);
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

	// Answers the question: if "opponent" is attacking the tile that this unit is standing on, does this unit defend instead of "otherDefender"?
	// Note that otherDefender does not necessarily belong to the same civ as this unit. Under standard Civ 3 rules you can't have units belonging
	// to two different civs on the same tile, but we don't want to assume that. In that case, whoever is an enemy of "opponent" should get
	// priority. Otherwise it's just whoever is stronger on defense.
	public bool HasPriorityAsDefender(MapUnit otherDefender, MapUnit opponent)
	{
		Player opponentPlayer = opponent.owner;
		bool weAreEnemy           = (opponentPlayer != null) ? ! opponentPlayer.IsAtPeaceWith(this.owner)          : false;
		bool otherDefenderIsEnemy = (opponentPlayer != null) ? ! opponentPlayer.IsAtPeaceWith(otherDefender.owner) : false;
		if (weAreEnemy && ! otherDefenderIsEnemy)
			return true;
		else if (otherDefenderIsEnemy && ! weAreEnemy)
			return false;
		else
			return (unitType.defense * hitPointsRemaining) > (otherDefender.unitType.defense * otherDefender.hitPointsRemaining);
	}

	public string Describe()
	{
		UnitPrototype type = this.unitType;
		string hPDesc = ((type.attack > 0) || (type.defense > 0)) ? $" ({hitPointsRemaining}/{maxHitPoints})" : "";
		string attackDesc = (type.bombard > 0) ? $"{type.attack}({type.bombard})" : type.attack.ToString();
		return $"{experienceLevel.displayName}{hPDesc} {type.name} ({attackDesc}.{type.defense}.{movementPointsRemaining}/{type.movement})";
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

	public struct Appearance {
		public AnimatedAction action;
		public TileDirection direction;
		public float progress; // Varies 0 to 1
		public float offsetX, offsetY; // Offset is in grid cells from the unit's location

		// When true, indicates that the animation is still playing (f.e. a unit is still running between tiles) so the UI shouldn't yet
		// autoselect another unit.
		public bool DeservesPlayerAttention()
		{
			// TODO: Special rules for different animations. We don't need to see workers do their thing but we do want to watch units
			// move. IMO we should also not show units fortifying even though I know the original game does.
			return progress < 1.0;
		}
	}

	public static MapUnit NONE = new MapUnit();
}

}
