using System.Collections.Generic;
using System.Text.Json.Serialization;
using C7GameData.AIData;

namespace C7GameData {
	/**
	 * A unit on the map.  Not to be confused with a unit prototype.
	 **/
	public class MapUnit {
		public ID id { get; internal set; }
		public UnitPrototype unitType { get; set; }
		public Player owner { get; set; }
		public Tile previousLocation { get; internal set; }
		private Tile currentLocation;

		public Tile location {
			get => currentLocation;
			set {
				previousLocation = location;
				currentLocation = value;
			}
		}
		public TilePath path { get; set; }

		public string experienceLevelKey;
		[JsonIgnore]
		public ExperienceLevel experienceLevel { get; set; }

		public MovementPoints movementPoints = new MovementPoints();
		public int hitPointsRemaining { get; set; }
		public int maxHitPoints {
			get {
				return experienceLevel.baseHitPoints; // TODO: Include bonus HP from unit type
			}
		}
		public bool isFortified { get; set; }
		//sentry, etc. will come later.  For now, let's just have a couple things so we can cycle through units that aren't fortified.
		public int defensiveBombardsRemaining;

		public TileDirection facingDirection;

		[JsonIgnore]
		public List<string> availableActions = new List<string>();
		public UnitAIData currentAIData;

		public MapUnit(ID id) {
			this.id = id;
		}

		internal MapUnit() {}

		public bool IsBusy() {
			return isFortified || (path != null && path.PathLength() > 0);
		}

		public override string ToString() {
			if (this != MapUnit.NONE) {
				return this.owner + " " + unitType.name + "at (" + location.xCoordinate + ", " + location.yCoordinate + ") with " + movementPoints.getMixedNumber() + " MP and " + hitPointsRemaining + " HP, id = " + id;
			} else {
				return "This is the NONE unit";
			}
		}

		public string Describe() {
			UnitPrototype type = this.unitType;
			string hPDesc = ((type.attack > 0) || (type.defense > 0)) ? $" ({hitPointsRemaining}/{maxHitPoints})" : "";
			string attackDesc = (type.bombard > 0) ? $"{type.attack}({type.bombard})" : type.attack.ToString();
			return $"{experienceLevel.displayName}{hPDesc} {type.name} ({attackDesc}.{type.defense}.{movementPoints.getMixedNumber()}/{type.movement})";
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
			public bool DeservesPlayerAttention() {
				// TODO: Special rules for different animations. We don't need to see workers do their thing but we do want to watch units
				// move. IMO we should also not show units fortifying even though I know the original game does.
				return progress < 1.0;
			}
		}

		public static MapUnit NONE = new MapUnit(ID.None("unit"));
	}
}
