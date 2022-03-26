namespace C7GameData

/*
    The save format is intended to be serialized to JSON upon saving
    and deserialized from JSON upon loading.

    The names are capitalized per C#, but I intend to use JsonSerializer
    settings to use camel case instead, unless there is reason not to.

*/
{
    using System.IO;
    using System.Text.Json;
    public class C7SaveFormat
    {
        public string Version = "v0.0early-prototype";
        // Rules is intended to be the analog to a BIC/X/Q
        public C7RulesFormat Rules;
        // This naming is probably bad form, but it makes sense to me to name it as such here
        public GameData GameData;
        public C7SaveFormat()
        {
	        GameData = new GameData();
        }
        public C7SaveFormat(GameData gameData, C7RulesFormat rules = null)
        {
            this.GameData = gameData;
            Rules = rules;
        }
        public static C7SaveFormat Load(string path)
        {
            string json = File.ReadAllText(path);
            C7SaveFormat save = JsonSerializer.Deserialize<C7SaveFormat>(json, JsonOptions);
            //Inflate things that are stored by reference
            foreach (Tile tile in save.GameData.map.tiles) {
                if (tile.ResourceKey == "NONE") {
                    tile.Resource = Resource.NONE;
                }
                else {
                    tile.Resource = save.GameData.Resources.Find(r => r.Key == tile.ResourceKey);
                }
                tile.baseTerrainType = save.GameData.terrainTypes.Find(t => t.Key == tile.baseTerrainTypeKey);
                tile.overlayTerrainType = save.GameData.terrainTypes.Find(t => t.Key == tile.overlayTerrainTypeKey);
            }
            return save;
        }
        public static void Save(C7SaveFormat save, string path)
        {
            string json = JsonSerializer.Serialize(save, JsonOptions);
            File.WriteAllText(path, json);
        }
        public static JsonSerializerOptions JsonOptions { get => new JsonSerializerOptions
        {
                // Lower-case the first letter in JSON because JSON naming standards
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // Pretty print during development; may change this for production
                WriteIndented = true,
                // By default it only serializes getters, this makes it serialize fields, too
                IncludeFields = true,
        };}

    }
}
