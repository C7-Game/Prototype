namespace C7Engine
{
    using C7GameData;
    public class TurnHandling
    {
        public static void EndTurn()
        {
            GameData gameData = EngineStorage.gameData;
            foreach (MapUnit mapUnit in gameData.mapUnits)
            {
                mapUnit.movementPointsRemaining = mapUnit.unitType.movement;
            }
            gameData.turn++;
        }
    }
}