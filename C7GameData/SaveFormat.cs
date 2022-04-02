namespace C7GameData

/*
    The save format is intended to be serialized to JSON upon saving
    and deserialized from JSON upon loading.

    The names are capitalized per C#, but I intend to use JsonSerializer
    settings to use camel case instead, unless there is reason not to.

*/
{
    using System.IO;
    using System.IO.Compression;
    using System.Text.Json;

    public enum SaveFileFormat
    {
        Plain,
        Zip,
    }

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
            GameData = gameData;
            Rules = rules;
        }

        public static C7SaveFormat Load(string path, SaveFileFormat format = SaveFileFormat.Plain)
        {
            System.Console.WriteLine("loading...");
            C7SaveFormat save = null;
            if (format == SaveFileFormat.Plain)
            {
                string json = File.ReadAllText(path);
                save = JsonSerializer.Deserialize<C7SaveFormat>(json, JsonOptions);
            }
            else
            {
                using (var fileStream = new FileStream(path, FileMode.Open))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry entry = archive.GetEntry("save");
                    using (Stream stream = entry.Open())
                    {
                        var task = JsonSerializer.DeserializeAsync<C7SaveFormat>(stream, JsonOptions).AsTask();
                        save = task.GetAwaiter().GetResult();
                    }
                }
            }

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

        public static void Save(C7SaveFormat save, string path, SaveFileFormat format = SaveFileFormat.Plain)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(save, JsonOptions);
            if (format == SaveFileFormat.Zip)
            {
                using (var zipStream = new MemoryStream())
                {
                    var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
                    ZipArchiveEntry entry = archive.CreateEntry("save");
                    using (Stream stream = entry.Open())
                    {
                        stream.Write(json, 0, json.Length);
                    }
                    archive.Dispose();
                    File.WriteAllBytes(path, zipStream.ToArray());
                }
            }
            else
            {
                File.WriteAllBytes(path, json);
            }
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
