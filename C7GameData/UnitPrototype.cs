namespace C7GameData
{
    /**
     * The prototype for a unit, which defines the characteristics of a unit.
     * For example, a Spearman might have 1 attack, 2 defense, and 1 movement.
     **/
    public class UnitPrototype : IProducible
    {
	    public string name { get; set; }
	    public int shieldCost { get; set; }
	    public int populationCost { get; set; }
        public int attack {get; set;}
        public int defense {get; set;}
        public int movement {get; set;}
        public int iconIndex {get; set;}

        public bool canFoundCity {get; set;}
        public bool canBuildRoads {get; set;}  //eventually there will be real worker tasks, for now let's just have one basic one.

        //probably some things like graphics and whether it's a helicopter someday

        public override string ToString()
        {
            return $"{name} ({attack}/{defense}/{movement})";
        }

        public MapUnit GetInstance()
        {
            MapUnit instance = new MapUnit();
            instance.unitType = this;
            instance.hitPointsRemaining = 3;    //todo: make this configurable
            instance.movementPointsRemaining = movement;
            return instance;
        }
    }
}
