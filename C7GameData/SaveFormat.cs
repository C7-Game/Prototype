namespace C7GameData

/*
    The save format is intended to be serialized to JSON upon saving
    and deserialized from JSON upon loading.

    The names are capitalized per C#, but I intend to use JsonSerializer
    settings to use camel case instead, unless there is reason not to.

    In my brief experience, only getters will show up in JSON output,
    but not e.g. int Value = 1;
*/
{
    using System;
    public class C7SaveFormat {
        public string Version { get; set; }
        public C7RulesFormat Rules { get; set; }
        public GameStateClass GameState { get; set; }
        public C7SaveFormat() {
            Version = "v0.0early-prototype";
        }
    }
    public class GameStateClass {
        public QueryCiv3.MapTile[] MapTiles { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
    }

}
