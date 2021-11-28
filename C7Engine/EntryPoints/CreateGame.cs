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
            // I think this path is relative from the C7 folder, not C7GameEngine, if that matters
            // TODO: Unsure if this will work when exported for distribution
            C7SaveFormat save = C7SaveFormat.Load(@"../C7GameData/c7-static-map-save.json");
            EngineStorage.setGameData(save.GameData);
            // possibly do something with save.Rules here when it exists
            // and maybe consider if we have any need to keep a reference to the save object handy...probably not

            var humanPlayer = save.GameData.createDummyGameData();
            return humanPlayer;
        }
    }
}
