namespace C7GameData
{
    using System;
    public class GameStateClass {
        public QueryCiv3.MapTile[] MapTiles { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
    }
    public class MockC7SaveFormat {
        public string Version { get; set; }
        public GameStateClass GameState { get; set; }
    }

}
