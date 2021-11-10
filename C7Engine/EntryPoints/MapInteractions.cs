using C7GameData;

namespace C7Engine
{
    public class MapInteractions
    {
        /**
         * Returns the whole map.  This seems like a lot, especially over a network.
         * We may want to think about how we want to design this.  Maybe you call this
         * once at the beginning, and thereafter make incremental updates?
         * For now, it's only called once at the beginning, which seems fine.
         **/
        public static GameMap GetWholeMap()
        {
            return EngineStorage.gameData.map;
        }
    }
}