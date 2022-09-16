using System.Collections.Generic;
using System.Linq;
using C7Engine.Pathing;
using Xunit;

namespace EngineTests {
	public class DijkstraAlgorithmTest {

		[Fact]
		void testOneVertexEmptyGraph() {
			TestGraph graph = new TestGraph();

			Dictionary<int, Edge<int>> result = DijkstrasAlgorithm.run(0, 0, graph);
			Assert.Equal(result.Count, 1);
			Assert.Equal(new Edge<int>(0, 0, 0.0f), result[0]);
		}

		[Fact]
		void testOneVertexGraphWithEdges() {
			TestGraph graph = new TestGraph();
			graph.edges.Add(new Edge<int>(0, 0, 1.0f));

			Dictionary<int, Edge<int>> result =
				DijkstrasAlgorithm.run(0, 0, graph, stopWhenReachDestination: true);
			Assert.Equal(result.Count, 1);
			Assert.Equal(new Edge<int>(0, 0, 0.0f), result[0]);

			result = DijkstrasAlgorithm.run(0, 0, graph, stopWhenReachDestination: false);
			Assert.Equal(result.Count, 1);
			Assert.Equal(new Edge<int>(0, 0, 0.0f), result[0]);
		}

		[Fact]
		void testThreeVertexGraph() {
			TestGraph graph = new TestGraph();
			graph.addBidirectional(0, 1, 3.0f);
			graph.addBidirectional(0, 2, 1.0f);
			graph.addBidirectional(1, 2, 1.0f);

			Dictionary<int, Edge<int>> result = DijkstrasAlgorithm.run(0, 1, graph);
			Assert.Equal(3, result.Count);

			Assert.Equal(new Edge<int>(0, 0, 0.0f), result[0]);
			Assert.Equal(new Edge<int>(0, 1, 2.0f), result[1]);
			Assert.Equal(new Edge<int>(1, 2, 1.0f), result[2]);
		}

		[Fact]
		void testReachDestinationFlag() {
			TestGraph graph = new TestGraph();
			graph.addBidirectional(0, 1, 1.0f);
			graph.addBidirectional(1, 2, 2.0f);
			graph.addBidirectional(2, 3, 3.0f);

			Dictionary<int, Edge<int>> withStop =
				DijkstrasAlgorithm.run(0, 0, graph, stopWhenReachDestination: true);
			Assert.Equal(1, withStop.Count);

			Dictionary<int, Edge<int>> dontStop =
				DijkstrasAlgorithm.run(0, 0, graph, stopWhenReachDestination: false);
			Assert.Equal(4, dontStop.Count);
		}
	}

	public class TestGraph: EdgeWalker<int> {
		public readonly List<Edge<int>> edges = new List<Edge<int>>();

		public void addBidirectional(int v1, int v2, float distance) {
			edges.Add(new Edge<int>(v1, v2, distance));
			edges.Add(new Edge<int>(v2, v1, distance));
		}

		public override IEnumerable<Edge<int>> getEdges(int node) {
			return edges.Where(t => t.prev == node);
		}
	}
}
