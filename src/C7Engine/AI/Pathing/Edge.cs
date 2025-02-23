using System;

namespace C7Engine.Pathing {
	public class Edge<TNode> : IComparable<Edge<TNode>> {
		public readonly TNode prev;
		public readonly TNode current;
		public float distanceToCurrent { get; private set; }

		public Edge(TNode prev, TNode current, float distanceToCurrent) {
			this.prev = prev;
			this.current = current;
			this.distanceToCurrent = distanceToCurrent;
		}

		public int CompareTo(Edge<TNode> other) {
			return distanceToCurrent.CompareTo(other.distanceToCurrent);
		}

		internal void addDistance(Edge<TNode> previous) {
			distanceToCurrent += previous.distanceToCurrent;
		}
	}
}
