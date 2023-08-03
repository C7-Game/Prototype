using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C7GameData {
	public class ID {
		private string key;

		private int n;

		public override string ToString() => $"{key}-{n}";

		public ID(string key, int n) {
			this.key = key;
			this.n = n;
		}

		public static ID FromString(string str) {
			string[] split  = str.Split('-', 2);
			string key = split[0];
			int n = int.Parse(split[1]);
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
}
