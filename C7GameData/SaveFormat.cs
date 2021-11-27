namespace C7GameData

/*
    The save format is intended to be serialized to JSON upon saving
    and deserialized from JSON upon loading.

    The names are capitalized per C#, but I intend to use JsonSerializer
    settings to use camel case instead, unless there is reason not to.

*/
{
    using System.Collections.Generic;
    public class C7SaveFormat
    {
        public string Version = "v0.0early-prototype";
        // Rules is intended to be the analog to a BIC/X/Q
        public C7RulesFormat Rules;
        // This naming is probably bad form, but it makes sense to me to name it as such here
        public GameData GameData;
        public C7SaveFormat(){}
        public C7SaveFormat(GameData gameData, C7RulesFormat rules = null)
        {
            this.GameData = gameData;
            Rules = rules;
        }
    }
}
