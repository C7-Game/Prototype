using System;
using System.Threading;

namespace C7GameData {

	public struct EntityId {
		private static int nextId = 0;

		private int id;
		private string context;

		public EntityId() {
			this.id = Interlocked.Increment(ref nextId);
		}

		public EntityId(string context) {
			this.context = context;
			this.id = Interlocked.Increment(ref nextId);
		}

		public override string ToString() {
			if (context == null) {
				return String.Format("entity_id_{0}", this.id);
			}
			return String.Format("{0}:{1}", this.context, this.id);
		}

		public override int GetHashCode() => this.id.GetHashCode();

		public override bool Equals(Object obj) => obj is EntityId && Equals((EntityId)obj);

		public bool Equals(EntityId other) => this.id == other.id;

		public static bool operator ==(EntityId lhs, EntityId rhs) => lhs.id == rhs.id;

		public static bool operator !=(EntityId lhs, EntityId rhs) => lhs.id != rhs.id;
	}
}
