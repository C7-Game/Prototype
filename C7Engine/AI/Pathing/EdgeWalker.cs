using System.Collections.Generic;
using C7GameData;

namespace C7Engine.Pathing {
	public abstract class EdgeWalker<TNode> {
		public abstract IEnumerable<Edge<TNode>> getEdges(TNode node);
	}

	public class UnitWalker : EdgeWalker<Tile> {
		private MapUnit unit;

		public UnitWalker(MapUnit unit) {
			this.unit = unit;
		}

		public override IEnumerable<Edge<Tile>> getEdges(Tile node) {
			bool landUnit = unit.IsLandUnit();

			List<Edge<Tile>> result = new List<Edge<Tile>>();
			foreach (KeyValuePair<TileDirection, Tile> pair in node.neighbors) {
				TileDirection direction = pair.Key;
				Tile neighbor = pair.Value;
				bool neighborHasCityWithSameOwner = neighbor.cityAtTile != null && neighbor.cityAtTile.owner == unit.owner;


				// Land units can only go on to land.
				if (landUnit && !neighbor.IsLand()) {
					continue;
				}

				// Water units can only go on water or coastal cities with the
				// same owner (to support canals).
				if (!landUnit && !(neighbor.IsWater() || neighborHasCityWithSameOwner)) {
					continue;
				}

				float tileMovementCost = TilePath.GetMovementCost(unit.owner, node, direction, neighbor);
				float unitMovementPoints = unit.unitType.movement;

				// If this tile would consume all of the movement points of this
				// unit, it has a cost of 1 turn. Otherwise we use the fraction
				// of a turn it would use as the cost.
				//
				// Examples:
				//  - Warrior (1mp) moving onto Grassland (cost 1) => 1 turn
				//  - Warrior (1mp) moving onto Hills (cost 2) => 1 turn
				//  - Warrior (1mp) moving onto Jungle (cost 3) => 1 turn
				//
				//  - Cavalry (3mp) moving onto Grassland (cost 1) => 1/3
				//  - Cavalry (3mp) moving onto Hills (cost 2) => 2/3
				//  - Cavalry (3mp) moving onto Jungle (cost 3) => 3/3
				//
				if (tileMovementCost >= unitMovementPoints) {
					result.Add(new Edge<Tile>(node, neighbor, 1));
				} else {
					result.Add(new Edge<Tile>(node, neighbor, tileMovementCost / unitMovementPoints));
				}
			}
			return result;
		}
	}
}
