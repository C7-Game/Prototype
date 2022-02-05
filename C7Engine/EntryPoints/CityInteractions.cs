namespace C7Engine
{
    using C7GameData;

    public class CityInteractions
    {
        //Eventually, this will need more info such as player
        public static void BuildCity(int x, int y, string playerGuid, string name)
        {
            GameData gameData = EngineStorage.gameData;

            Player owner = gameData.players.Find(player => player.guid == playerGuid);

            City newCity = new City(x, y, owner, name);
            gameData.cities.Add(newCity);

            Tile tileWithNewCity = EngineStorage.gameData.map.tileAt(x, y);
            tileWithNewCity.cityAtTile = newCity;
        }
    }
}
