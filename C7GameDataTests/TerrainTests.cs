using System;
using System.IO;
using System.Text.Json;
using C7GameData;
using Xunit;

public class TerrainTest
{
	private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions{
		IncludeFields = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	private readonly string terrainJsonPath = "../../../../C7/Text/terrain.json";

	[Fact]
	public void LoadDefaultTerrainJson() {
		TerrainFile terrain = null;
		using (FileStream fs = File.OpenRead(terrainJsonPath)) {
			terrain = JsonSerializer.Deserialize<TerrainFile>(fs, this.jsonSerializerOptions);
		}

		Assert.Equal("civ3", terrain.Metadata.Mod);
		Assert.NotEmpty(terrain.Types);
		Assert.True(terrain.Types.ContainsKey("hills"));
		Assert.True(terrain.Types["hills"].hilly);
		Assert.Equal("civ3/hills", terrain.Types["hills"].Key);
		Assert.True(terrain.Types.ContainsKey("plains"));
		Assert.False(terrain.Types["plains"].hilly);
		Assert.Equal("civ3/plains", terrain.Types["plains"].Key);
	}
}
