using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// Converter for serializing 2D array types with System.Text.Json
// The implementation is copied from:
// https://stackoverflow.com/questions/66280645/how-can-i-serialize-a-double-2d-array-to-json-using-system-text-json

public class Json2DArrayConverter : JsonConverterFactory {
	public override bool CanConvert(Type typeToConvert) => typeToConvert.IsArray && typeToConvert.GetArrayRank() == 2;

	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) {
		return (JsonConverter)Activator.CreateInstance(
			typeof(Json2DArrayConverterInner<>).MakeGenericType(new[] { type.GetElementType() }),
			BindingFlags.Instance | BindingFlags.Public,
			binder: null,
			args: new object[] { options },
			culture: null
		);
	}

	class Json2DArrayConverterInner<T> : JsonConverter<T[,]> {
		readonly JsonConverter<T> _valueConverter;

		public Json2DArrayConverterInner(JsonSerializerOptions options) {
			if (typeof(T) != typeof(object)) {
				// encountered a bug using the builtin ObjectConverter
				this._valueConverter = (JsonConverter<T>)options.GetConverter(typeof(T));
			}
		}

		public override void Write(Utf8JsonWriter writer, T[,] array, JsonSerializerOptions options) {
			// Adapted from this answer https://stackoverflow.com/a/25995025/3744182
			// By https://stackoverflow.com/users/3258160/pedro
			// To https://stackoverflow.com/questions/21986909/convert-multidimensional-array-to-jagged-array-in-c-sharp
			int rowsFirstIndex = array.GetLowerBound(0);
			int rowsLastIndex = array.GetUpperBound(0);
			int columnsFirstIndex = array.GetLowerBound(1);
			int columnsLastIndex = array.GetUpperBound(1);

			writer.WriteStartArray();
			for (int i = rowsFirstIndex; i <= rowsLastIndex; i++) {
				writer.WriteStartArray();
				for (int j = columnsFirstIndex; j <= columnsLastIndex; j++) {
					_valueConverter.WriteOrSerialize(writer, array[i, j], options);
				}
				writer.WriteEndArray();
			}
			writer.WriteEndArray();
		}

		public override T[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			return JsonSerializer.Deserialize<List<List<T>>>(ref reader, options)?.To2D();
		}
	}
}

public static partial class ArrayExtensions {
	public static T[,] To2D<T>(this List<List<T>> source) {
		// Adapted from this answer https://stackoverflow.com/a/26291720/3744182
		// By https://stackoverflow.com/users/3909293/diligent-key-presser
		// To https://stackoverflow.com/questions/26291609/converting-jagged-array-to-2d-array-c-sharp
		var firstDim = source.Count;
		var secondDim = source.Select(row => row.Count).FirstOrDefault();
		if (!source.All(row => row.Count == secondDim)) {
			throw new InvalidOperationException();
		}
		var result = new T[firstDim, secondDim];
		for (var i = 0; i < firstDim; i++) {
			for (int j = 0, count = source[i].Count; j < count; j++) {
				result[i, j] = source[i][j];
			}
		}
		return result;
	}
}

public static class JsonSerializerExtensions {
	public static void WriteOrSerialize<T>(this JsonConverter<T> converter, Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
		if (converter != null) {
			converter.Write(writer, value, options);
		} else {
			JsonSerializer.Serialize(writer, value, typeof(T), options);
		}
	}
}
