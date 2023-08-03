using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C7GameData {
	public class ID {
		// at runtime, any ID parsed from a string as "<key>-none" will be
		// compared to other IDs by -0xC7C7
		private static readonly int magicNoneIdNumber = -0xC7C7;

		private string key;

		private int n;

		public override string ToString() {
			return n != magicNoneIdNumber ? $"{key}-{n}" : $"{key}-none";
		}

		internal ID(string key, int n) {
			this.key = key;
			this.n = n;
		}

		public static ID None(string key) {
			return new ID(key, magicNoneIdNumber);
		}

		public static ID FromString(string str) {
			string[] split  = str.Split('-', 2);
			string key = split[0];
			int n = split[1] == "none" ? magicNoneIdNumber : int.Parse(split[1]);
			if (n < 0) {
				throw new Exception($"ID cannot have a negative number, got {n}");
			}
			return new ID(key, n);
		}

		public override bool Equals(object obj) {
			return obj switch {
				null => false,
				ID id => id.n == n && id.key == key,
				_ => false,
			};
		}

		// c# string hash is based on value, not reference
		public override int GetHashCode() => ToString().GetHashCode();

		public static bool operator ==(ID lhs, ID rhs) {
			if (lhs is null || rhs is null) {
				return false;
			}
			return lhs.n == rhs.n ? lhs.key == rhs.key : false;
		}

		public static bool operator !=(ID lhs, ID rhs) {
			return !(lhs == rhs);
		}
	}

	public class IDJsonConverter : JsonConverter<ID> {
		public override void Write(Utf8JsonWriter writer, ID id, JsonSerializerOptions options) => writer.WriteStringValue(id.ToString());

		public override ID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => ID.FromString(reader.GetString());
	}

	public class IDFactory {
		private Dictionary<string, int> keyCounter;

		// TODO: when creating an IDFactory for a loaded save game, take care
		// to find the largest n-value for each ID key.
		// ie. loading a save with units ["warrior-1", "warrior-3", "worker-2"]
		//     should result in an id factory with
		// keyCounter = {
		//     "warrior": 3,
		//     "worker": 2,
		// }
		public IDFactory() {
			keyCounter = new Dictionary<string, int>();
		}

		public ID CreateID(string key) {
			int n = keyCounter.GetValueOrDefault(key, 1);
			keyCounter[key] = n + 1;
			return new ID(key, n);
		}
	}
}
