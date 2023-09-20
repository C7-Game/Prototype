using System.Collections.Generic;
using System.Linq;

namespace C7GameData.Save {

	public class SaveUnit {
		public ID id;
		public string prototype;
		public ID owner;
		public TileLocation previousLocation = new TileLocation();
		public TileLocation currentLocation;
		public List<TileLocation> path;
		public int hitPointsRemaining;
		public float movePointsRemaining;
		public string action; // "fortified"
		public TileDirection facingDirection;
		public string experience;
		public SaveUnit() { }

		public SaveUnit(MapUnit unit, GameMap map) {
			id = unit.id;
			prototype = unit.unitType.name;
			owner = unit.owner.id;
			if (unit.previousLocation is not null) {
				previousLocation = new TileLocation(unit.previousLocation);
			}
			currentLocation = new TileLocation(unit.location);
			if (unit.path?.PathLength() > 0) {
				path = unit.path.path.ToList().ConvertAll(tile => new TileLocation(tile));
			}
			hitPointsRemaining = unit.hitPointsRemaining;
			action = unit.isFortified ? "fortified" : "";
			facingDirection = unit.facingDirection;
			experience = unit.experienceLevelKey;
			movePointsRemaining = unit.movementPoints.remaining;
		}

		public MapUnit ToMapUnit(List<UnitPrototype> prototypes, List<ExperienceLevel> experienceLevels, List<Player> players, GameMap map) {
			MapUnit unit = new MapUnit{
				id = id,
				unitType = prototypes.Find(p => p.name == prototype),
				experienceLevelKey = experience,
				experienceLevel = experienceLevels.Find(el => el.key == experience),
				owner = players.Find(player => player.id == owner),
				location = map.tileAt(currentLocation.x, currentLocation.y),
				previousLocation = currentLocation.x == - 1 ? Tile.NONE : map.tileAt(previousLocation.x, previousLocation.y),
				hitPointsRemaining = hitPointsRemaining,
				movementPoints = new MovementPoints(),
				isFortified = action == "fortified",
			};
			unit.location.unitsOnTile.Add(unit);
			unit.movementPoints.reset(movePointsRemaining);
			return unit;
		}
	}
}
