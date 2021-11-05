namespace C7GameData
{
    using System;
    /**
     * A unit on the map.  Not to be confused with a unit prototype.
     **/
    public class MapUnit
    {
        public string guid  {get;}
        public UnitPrototype unitType {get; set;}
        public Tile location {get; set;}

        public int movementPointsRemaining {get; set;}
        public bool isFortified {get; set;}
        //sentry, etc. will come later.  For now, let's just have a couple things so we can cycle through units that aren't fortified.

        public MapUnit()
        {
            guid = Guid.NewGuid().ToString();
        }
    }
}