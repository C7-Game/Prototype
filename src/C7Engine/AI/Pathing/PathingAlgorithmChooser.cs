using C7GameData;

namespace C7Engine.Pathing {
	/**
	 * Returns a pathing algorithm to use.
	 */
	public class PathingAlgorithmChooser {
		public static PathingAlgorithm GetAlgorithm(MapUnit unit) {
			return new AStarAlgorithm(new UnitWalker(unit), (Tile from, Tile to) => {
				// HACK: for land-based movement we have to deal with railroads,
				// which have zero movement cost. If our heuristic is too strong it
				// will result in units taking a direct path between points A and B,
				// even if a more indirect path could be taken entirely by railroad.
				// To avoid this problem we scale our heuristic function down by a
				// constant (arbitraily chosen to work well in practice) so that a 
				// typical tile movement cost (around 1/3 to 3, depending on roads
				// and terrain) dwarfs the heuristic. The heuristic is still enough
				// to point the search in the proper direction, and since it is 
				// still an underestimate in most cases, it works properly.
				if (unit.IsLandUnit()) {
					return from.distanceTo(to) / 100.0;
				}

				return from.distanceTo(to);
			});
		}
	}
}
