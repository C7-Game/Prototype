namespace C7Engine
{
    using C7GameData;
    public class TurnHandling
    {
        public static void EndTurn()
        {
            GameData gameData = EngineStorage.gameData;
            //Reset movement points available for all units
            foreach (MapUnit mapUnit in gameData.mapUnits)
            {
                mapUnit.movementPointsRemaining = mapUnit.unitType.movement;
            }
            //Clear all wait queue, so if a player ended the turn without handling all waited units, they are selected
            //at the same place in the order.  Confirmed this is what Civ3 does.
            UnitInteractions.ClearWaitQueue();
            gameData.turn++;
        }
    }
}