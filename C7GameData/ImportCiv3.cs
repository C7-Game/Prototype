namespace C7GameData

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/
{
    // Additional parameters used to refer to specic media files and tiles in Civ3
    public class Civ3Tile : Tile
    {
        public int BaseTerrainFileID { get; set; }
        public int BaseTerrainImageID { get; set; }
    }
    public class ImportCiv3
    {
        static public C7SaveFormat ImportSav()
        {
            C7SaveFormat c7Save = new C7SaveFormat();
            return c7Save;
        }
        static public C7RulesFormat ImportBic()
        {
            C7RulesFormat c7Save = new C7RulesFormat();
            return c7Save;
        }
    }
}
