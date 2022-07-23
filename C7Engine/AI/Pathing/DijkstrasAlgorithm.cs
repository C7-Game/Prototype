using System.Collections.Generic;
using System.Linq;
using C7GameData;

namespace C7Engine.Pathing {

	public class DijkstrasAlgorithm: PathingAlgorithm {
		private readonly EdgeWalker<Tile> edgeWalker;

		public DijkstrasAlgorithm(EdgeWalker<Tile> edgeWalker) {
			this.edgeWalker = edgeWalker;
		}

		public override TilePath PathFrom(Tile start, Tile destination) {
			Dictionary<Tile, Edge<Tile>> walkingMap = run(start, destination, edgeWalker);
			return makePath(walkingMap, destination);
		}

		/**
		 * return Dictionary: Node -> Edge(previousNode, node, distanceToNodeFromStart)
		 * with stopWhenReachDestination == false it calculate distances to each available point
		 */
		public static Dictionary<TNode, Edge<TNode>> run<TNode>(TNode start, TNode destination,
			EdgeWalker<TNode> edgeWalker, bool stopWhenReachDestination = false)
		{
			Dictionary<TNode, Edge<TNode>> visitedNodes = new Dictionary<TNode, Edge<TNode>>();
			BinaryMinHeap<Edge<TNode>> edgesQueue = new BinaryMinHeap<Edge<TNode>>();

			edgesQueue.insert(new Edge<TNode>(start, start, 0.0f));

			while (edgesQueue.count > 0) {
				Edge<TNode> edge = edgesQueue.extract();

				if (!visitedNodes.ContainsKey(edge.current)) {
					visitedNodes[edge.current] = edge;
					foreach (Edge<TNode> newEdge in edgeWalker.getEdges(edge.current)) {
						newEdge.addDistance(edge);
						edgesQueue.insert(newEdge);
					}
				}

				if (stopWhenReachDestination && edge.current.Equals(destination)) break;
			}

			return visitedNodes;
		}

		private static TilePath makePath(Dictionary<Tile, Edge<Tile>> walkingMap, Tile destination) {
			if (!walkingMap.ContainsKey(destination)) {
				return TilePath.EmptyPath(destination);
			}

			List<Tile> tilesInPath = new List<Tile>() { destination };
			while (true) {
				Edge<Tile> edge = walkingMap[destination];
				if (edge.prev.Equals(destination)) {
					break;
				}

				destination = edge.prev;
				tilesInPath.Add(destination);
			}

			tilesInPath.Reverse();
			Queue<Tile> path = new Queue<Tile>();
			foreach (Tile t in tilesInPath.Skip(1)) {
				path.Enqueue(t);
			}
			return new TilePath(destination, path);
		}
	}
}
