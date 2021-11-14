namespace C7GameData
{
    using System;
    using System.Collections.Generic;
    /**
     * A unit on the map.  Not to be confused with a unit prototype.
     **/
    public class MapUnit
    {
        public string guid  {get;}
        public UnitPrototype unitType {get; set;}
        public Player owner {get; set;}
        public Tile location {get; set;}

        public int movementPointsRemaining {get; set;}
        public int hitPointsRemaining {get; set;}
        public int maxHitPoints {
            get {
                return 3; // Eventually we'll add HP from experience and the type's inherent bonus
            }
        }
        public bool isFortified {get; set;}
        //sentry, etc. will come later.  For now, let's just have a couple things so we can cycle through units that aren't fortified.

        //This probably should not be serialized.  In .NET, we'd add the [ScriptIgnore] and using System.Web.Script.Serialization.
        //But .NET Core doesn't support that.  So, we'll have to figure out something else.  Maybe a library somewhere.
        public List<string> availableActions = new List<string>();

        public MapUnit()
        {
            guid = Guid.NewGuid().ToString();
        }

        public static MapUnit NONE = new MapUnit();
    }
}
