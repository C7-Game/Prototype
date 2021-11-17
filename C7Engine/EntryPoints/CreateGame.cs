namespace C7Engine
{
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
            GameData gameData = new GameData();
            var humanPlayer = gameData.createDummyGameData(terrainGen);

            EngineStorage.setGameData(gameData);

            return humanPlayer;
        }
    }
}
