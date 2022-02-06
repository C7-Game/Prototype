using System;

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
        public static Player createGame(string loadFilePath, string defaultBicPath)
        {
            C7SaveFormat save;
            if (loadFilePath.EndsWith("SAV", System.StringComparison.CurrentCultureIgnoreCase))
            {
                save = ImportCiv3.ImportSav(loadFilePath, defaultBicPath);
            }
            else if (loadFilePath.EndsWith("BIQ", System.StringComparison.CurrentCultureIgnoreCase)) {
                //Probably need an ImportBiq variant someday.  Maybe.
                save = ImportCiv3.ImportSav(loadFilePath, defaultBicPath);
            }
            else {
                save = C7SaveFormat.Load(loadFilePath);
            }
            save.GameData.PerformPostLoadActions();
            EngineStorage.setGameData(save.GameData);
			//If we are loading from JSON and it lacks an RNG, set one
			//This should be a temporary hack until we have a more stable C7 default rule set.
			if (save.GameData.rng == null) {
				save.GameData.rng = new Random();
			}
            // possibly do something with save.Rules here when it exists
            // and maybe consider if we have any need to keep a reference to the save object handy...probably not

            var humanPlayer = save.GameData.CreateDummyGameData();
            return humanPlayer;
        }
    }
}
