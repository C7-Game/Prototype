using System.Collections.Generic;
using System.Linq;

namespace C7GameData.Save {

	public class SaveUnit : IHasID {
		public ID id { get; set; }
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
		public float WorkerProgressTowardsJob;
		public ID WorkerJob;

		// True for multiple types of automation, including worker automation
		// and automated exploring.
		public bool isAutomated;

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
			isAutomated = unit.isAutomated;
			facingDirection = unit.facingDirection;
			experience = unit.experienceLevelKey;
			movePointsRemaining = unit.movementPoints.remaining;
			WorkerProgressTowardsJob = unit.WorkerProgressTowardsJob;
			WorkerJob = unit.WorkerJob?.Id;
		}


		public MapUnit ToMapUnit(List<UnitPrototype> prototypes, List<ExperienceLevel> experienceLevels, List<Player> players, List<Terraform> terraforms, GameMap map) {
			MapUnit unit = new MapUnit{
				id = id,
				unitType = prototypes.Find(p => p.name == prototype),
				experienceLevelKey = experience,
				experienceLevel = experienceLevels.Find(el => el.key == experience),
				owner = players.Find(player => player.id == owner),
				location = map.tileAt(currentLocation.X, currentLocation.Y),
				previousLocation = currentLocation.X == - 1 ? Tile.NONE : map.tileAt(previousLocation.X, previousLocation.Y),
				hitPointsRemaining = hitPointsRemaining,
				movementPoints = new MovementPoints(),
				isFortified = action == "fortified",
				isAutomated = isAutomated,
				facingDirection = facingDirection,
				WorkerProgressTowardsJob = WorkerProgressTowardsJob,
				WorkerJob = WorkerJob == null ? null:terraforms.Find(tf => tf.Id == WorkerJob)
			};
			unit.location.unitsOnTile.Add(unit);
			unit.movementPoints.reset(movePointsRemaining);

			return unit;
		}
	}
}
