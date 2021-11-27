namespace C7Engine
{
    using System.IO;
    using System.Text.Json;
    using C7GameData;
    public class CreateGame
    {
        /**
         * For now, I'm making the methods that the C7 client can call be static.
         * We may want a different solution in the end, but this lets us start prototyping
         * quickly.  By keeping all the client-callable APIs in the EntryPoints folder,
         * hopefully it won't be too much of a goose hunt to refactor it later if we decide to do so.
         **/
        public static Player createGame(GameMap.TerrainNoiseMapGenerator terrainGen)
        {
            // GameData gameData = new GameData();
            // var humanPlayer = gameData.createDummyGameData(terrainGen);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
                // PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = File.ReadAllText(@"../C7GameData/c7-static-map-save.json");
            C7SaveFormat save = JsonSerializer.Deserialize<C7SaveFormat>(json, jsonOptions);
            EngineStorage.setGameData(save.GameData);

            // In a Civ3 save, player 0 is barbs and player 1 is first human player
            return save.GameData.players.ToArray()[1];
        }
    }
}
