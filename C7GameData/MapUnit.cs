namespace C7GameData
{
    /**
     * A unit on the map.  Not to be confused with a unit prototype.
     **/
    public class MapUnit
    {
        UnitPrototype typeOfUnit;
        Tile location;

        int movementPointsRemaining;
        boolean isFortified;
        //sentry, etc. will come later.  For now, let's just have a couple things so we can cycle through units that aren't fortified.
    }
}