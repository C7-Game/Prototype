using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C7GameData {
	/**
	 * Represents something that can be produced by a city.
	 * Known examples are Buildings and UnitPrototypes.
	 */
	public interface IProducible {
		string name { get; set; }
		int shieldCost { get; set; }
		int populationCost { get; set; }
	}

	public class IProducibleJsonConverter : JsonConverter<IProducible> {
		private Func<string, IProducible> lookup;
		public IProducibleJsonConverter(Func<string, IProducible> lookupIproducible) {
			lookup = lookupIproducible;
		}

		public override IProducible Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			string name = reader.GetString();
			if (name is null) {
				return null;
			}
			return lookup(name);
		}

		public override void Write(Utf8JsonWriter writer, IProducible value, JsonSerializerOptions options) {
			if (value is not null) {
				writer.WriteStringValue(value.name);
			}
		}
	}
}
