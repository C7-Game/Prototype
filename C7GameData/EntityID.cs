using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C7GameData {
	public class EntityID {
		[JsonIgnore]
		private static object countsLock = new object();
		[JsonIgnore]
		private static Dictionary<string, int> counts = new Dictionary<string, int>();
		public string key;
		private int number;

		private EntityID() {}

		private static void updateLargestKeyCount(string key, int count) {
			lock (countsLock) {
				if (count > counts.GetValueOrDefault(key, -1)) {
					counts[key] = count;
				}
			}
		}

		private static int getNextNumberForKey(string key) {
			int count;
			lock (countsLock) {
				count = counts.GetValueOrDefault(key, -1) + 1;
				counts[key] = count;
			}
			return count;
		}

		// used for deserialization
		public static EntityID FromString(string id) {
			EntityID entityID = new EntityID();
			string[] split = id.Split("-", 2);
			if (split.Length != 2) {
				throw new ArgumentException($"invalid id format: {id}");
			}
			entityID.key = split[0];
			int.TryParse(split[1], out entityID.number);
			// need to track largest key count so that new id's are unique
			updateLargestKeyCount(entityID.key, entityID.number);
			return entityID;
		}

		public static EntityID New(string key) {
			return new EntityID{
				key = key,
				number = getNextNumberForKey(key),
			};
		}

		public static bool operator ==(EntityID lhs, EntityID rhs) {
			if (lhs is null || rhs is null) {
				return false;
			}
			return lhs.number != rhs.number ? false : lhs.key == rhs.key;
		}

		public static bool operator !=(EntityID lhs, EntityID rhs) {
			return !(lhs == rhs);
		}

		public override bool Equals(object obj) {
			return obj switch {
				EntityID e => this == e,
				_ => false,
			};
		}

		public override int GetHashCode() {
			return ToString().GetHashCode();
		}

		public override string ToString() {
			return $"{key}-{number}";
		}
	}

	public class EntityIDJsonConverter : JsonConverter<EntityID> {
		public override void Write(Utf8JsonWriter writer, EntityID id, JsonSerializerOptions options) => writer.WriteStringValue(id.ToString());

		public override EntityID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => EntityID.FromString(reader.GetString());
	}
}
