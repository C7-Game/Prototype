using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using MoonSharp.Interpreter;

namespace C7Engine.Lua;

public class JsonConverter {
	private readonly Script script;
	private readonly Table arrayMetatable;
	private readonly Table objectMetatable;

	public JsonConverter(Script script) {
		this.script = script ?? throw new ArgumentNullException(nameof(script));

		// Create metatables for marking arrays and objects
		arrayMetatable = new Table(this.script);
		arrayMetatable["__type"] = "array";

		objectMetatable = new Table(this.script);
		objectMetatable["__type"] = "object";
	}

	/// <summary>
	/// Decodes JSON string into MoonSharp DynValue
	/// </summary>
	public DynValue Decode(string json) {
		if (string.IsNullOrEmpty(json))
			return DynValue.Nil;

		try {
			using JsonDocument doc = JsonDocument.Parse(json);
			return ConvertJsonElementToDynValue(doc.RootElement);
		} catch (JsonException ex) {
			throw new ArgumentException($"Invalid JSON: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Encodes MoonSharp DynValue into JSON string
	/// </summary>
	public string Encode(DynValue value, bool prettyPrint = false) {
		JsonWriterOptions options = new JsonWriterOptions
		{
			Indented = prettyPrint
		};

		using System.IO.MemoryStream stream = new System.IO.MemoryStream();
		using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, options)) {
			WriteJsonValue(writer, value);
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	private DynValue ConvertJsonElementToDynValue(JsonElement element) {
		switch (element.ValueKind) {
			case JsonValueKind.Object:
				return ConvertJsonObjectToTable(element);

			case JsonValueKind.Array:
				return ConvertJsonArrayToTable(element);

			case JsonValueKind.Number:
				return DynValue.NewNumber(element.GetDouble());

			case JsonValueKind.String:
				return DynValue.NewString(element.GetString());

			case JsonValueKind.True:
				return DynValue.NewBoolean(true);

			case JsonValueKind.False:
				return DynValue.NewBoolean(false);

			case JsonValueKind.Null:
			case JsonValueKind.Undefined:
				return DynValue.Nil;

			default:
				return DynValue.Nil;
		}
	}

	private DynValue ConvertJsonObjectToTable(JsonElement element) {
		Table table = new Table(script);

		// Set metatable to mark this as an object
		table.MetaTable = objectMetatable;

		foreach (JsonProperty property in element.EnumerateObject()) {
			table[property.Name] = ConvertJsonElementToDynValue(property.Value);
		}

		return DynValue.NewTable(table);
	}

	private DynValue ConvertJsonArrayToTable(JsonElement element) {
		Table table = new Table(script);

		// Set metatable to mark this as an array
		table.MetaTable = arrayMetatable;

		// Lua arrays are 1-indexed
		int index = 1;
		foreach (JsonElement item in element.EnumerateArray()) {
			table[index++] = ConvertJsonElementToDynValue(item);
		}

		return DynValue.NewTable(table);
	}

	private void WriteJsonValue(Utf8JsonWriter writer, DynValue value) {
		switch (value.Type) {
			case DataType.Nil:
			case DataType.Void:
				writer.WriteNullValue();
				break;

			case DataType.Boolean:
				writer.WriteBooleanValue(value.Boolean);
				break;

			case DataType.Number:
				// Check if it's an integer
				if (Math.Abs(value.Number % 1) < double.Epsilon)
					writer.WriteNumberValue((long)value.Number);
				else
					writer.WriteNumberValue(value.Number);
				break;

			case DataType.String:
				writer.WriteStringValue(value.String);
				break;

			case DataType.Table:
				WriteJsonTable(writer, value.Table);
				break;

			default:
				writer.WriteNullValue();
				break;
		}
	}

	private void WriteJsonTable(Utf8JsonWriter writer, Table table) {
		if (table == null) {
			writer.WriteNullValue();
			return;
		}

		// Check metatable to determine if it's an array or object
		bool isArray = IsArray(table);

		if (isArray) {
			writer.WriteStartArray();

			// Get max numeric key to handle sparse arrays
			System.Collections.Generic.List<int> keys = table.Keys.Where(k => k.Type == DataType.Number)
								.Select(k => (int)k.Number)
								.OrderBy(k => k)
								.ToList();

			if (keys.Count > 0) {
				int maxKey = keys.Max();

				// Build array with proper indexing (Lua is 1-indexed)
				for (int i = 1; i <= maxKey; i++) {
					DynValue val = table.Get(i);
					WriteJsonValue(writer, val);
				}
			}

			writer.WriteEndArray();
		} else {
			writer.WriteStartObject();

			foreach (TablePair pair in table.Pairs) {
				string key;

				if (pair.Key.Type == DataType.String) {
					key = pair.Key.String;
				} else if (pair.Key.Type == DataType.Number) {
					key = pair.Key.Number.ToString();
				} else {
					continue; // Skip non-string/number keys
				}

				writer.WritePropertyName(key);
				WriteJsonValue(writer, pair.Value);
			}

			writer.WriteEndObject();
		}
	}

	private bool IsArray(Table table) {
		// Check metatable first
		if (table.MetaTable != null) {
			DynValue typeValue = table.MetaTable.Get("__type");
			if (typeValue.Type == DataType.String) {
				return typeValue.String == "array";
			}
		}

		// Fallback: heuristic check
		// An array has only consecutive numeric keys starting from 1
		System.Collections.Generic.List<DynValue> keys = table.Keys.ToList();

		if (keys.Count == 0) {
			// Empty table - default to object unless it has array metatable
			return false;
		}

		// Check if all keys are numbers
		if (!keys.All(k => k.Type == DataType.Number))
			return false;

		System.Collections.Generic.List<int> numericKeys = keys.Select(k => (int)k.Number).OrderBy(k => k).ToList();

		// Check if keys start at 1 and are consecutive
		for (int i = 0; i < numericKeys.Count; i++) {
			if (numericKeys[i] != i + 1)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Creates an empty array table with proper metatable
	/// </summary>
	public DynValue CreateEmptyArray() {
		Table table = new Table(script);
		table.MetaTable = arrayMetatable;
		return DynValue.NewTable(table);
	}

	/// <summary>
	/// Creates an empty object table with proper metatable
	/// </summary>
	public DynValue CreateEmptyObject() {
		Table table = new Table(script);
		table.MetaTable = objectMetatable;
		return DynValue.NewTable(table);
	}
}
