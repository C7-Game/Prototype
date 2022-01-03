using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    public enum CultureGroup {
        None,
        American,
        European,
        Mediterranean,
        MidEast,
        Asian,
    }

    // A playable race
    public class Race
    {
        public List<Trait> traits;
        public List<CityCitizen> cityCitizens;
        public List<TileUnit> units;
        public List<Unit> allowedUnits;
        public Government favoredGovernment;
        public Government scornedGovernment;
        public Civ civ; // Could make this a list if we want to allow multiple civs of the same race at once!
        public CultureGroup cultureGroup;
        public Unit kingUnit;

        public string leaderName;
        public string leaderTitle;
        public bool leaderGender;
        public string name; // (eg. Russia)
        public string peoplesName; // (eg. Russians)
        public string adjective; // (eg. Russian)
        public int gender; // (masculine, feminine, neuter)
        public List<string> cityNames;
        public List<string> greatLeaderNames;
        public List<string> scientificLeaderNames;
        public int color;
        public int backupColor;
        public int aggressionLevel;
        public string civilopediaEntry;
        public string animationFilename; // this needs to be broken up by era and by forward/reverse
    }
}
