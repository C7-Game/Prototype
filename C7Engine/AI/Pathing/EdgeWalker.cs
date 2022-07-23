using System.Collections.Generic;
using C7GameData;

namespace C7Engine.Pathing {
	public abstract class EdgeWalker<TNode>
	{
		public abstract IEnumerable<Edge<TNode>> getEdges(TNode node);
	}

	public class WalkerOnLand: EdgeWalker<Tile> {
		public override IEnumerable<Edge<Tile>> getEdges(Tile node) {
			List<Edge<Tile>> result = new List<Edge<Tile>>();
			foreach (KeyValuePair<TileDirection, Tile> pair in node.neighbors) {
				TileDirection direction = pair.Key;
				Tile neighbor = pair.Value;
				if (neighbor.IsLand()) {
					float movementCost = MapUnitExtensions.getMovementCost(neighbor, direction, neighbor);
					result.Add(new Edge<Tile>(node, neighbor, movementCost));
				}
			}
			return result;
		}
	}
}
