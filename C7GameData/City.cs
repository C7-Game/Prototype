namespace C7GameData
{
    using System;
    public class City
    {
        public string guid {get;}
        public int xLocation {get;}
        public int yLocation {get;}
        public string name;

        public City(int x, int y, string name)
        {
            guid = Guid.NewGuid().ToString();
            this.xLocation = x;
            this.yLocation = y;
            this.name = name;
        }
    }
}