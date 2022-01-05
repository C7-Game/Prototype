using C7GameData;

namespace C7Engine
{
    public class MapInteractions
    {
        public static Tile GetTileAt(int tileX, int tileY)
        {
            return EngineStorage.gameData.map.tileAt(tileX, tileY);
        }
    }
}
