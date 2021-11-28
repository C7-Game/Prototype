namespace C7GameData
{
    using System;
    public class City
    {
        public string guid {get;}
        public int xLocation {get;}
        public int yLocation {get;}
        public string name;

        public Player owner {get; set;}

        public City(int x, int y, Player owner, string name)
        {
            guid = Guid.NewGuid().ToString();
            this.xLocation = x;
            this.yLocation = y;
            this.owner = owner;
            this.name = name;
        }

        public bool IsCapital()
        {
            //TODO: Look through built cities, figure out if it is or not
            return false;
        }
    }
}