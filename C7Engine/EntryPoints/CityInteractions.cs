namespace C7Engine
{
    using C7GameData;

    public class CityInteractions
    {
        //Eventually, this will need more info such as player
        public static void BuildCity(int x, int y, string name)
        {
            GameData gameData = EngineStorage.gameData;

            City newCity = new City(x, y, name);
            gameData.cities.Add(newCity);

            Tile tileWithNewCity = MapInteractions.GetTileAt(x, y);
            tileWithNewCity.cityAtTile = newCity;
        }
    }
}