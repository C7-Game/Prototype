namespace C7GameData

/*
    The save format is intended to be serialized to JSON upon saving
    and deserialized from JSON upon loading.

    The names are capitalized per C#, but I intend to use JsonSerializer
    settings to use camel case instead, unless there is reason not to.

*/
{
    using System.Collections.Generic;
    public class C7SaveFormat : GameData
    {
        public string Version;
        // Trying to use GameData object which obviate these
        // public C7RulesFormat Rules;
        // public GameStateClass GameState;
        public C7SaveFormat()
        {
            Version = "v0.0early-prototype";
        }
    }
}