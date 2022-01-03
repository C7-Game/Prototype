using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // The overall container for all game state data
    public class Game
    {
        public Rules rules;
        public Map map;
        public List<Resource> resources;
        public List<Unit> units;
        public List<Difficulty> difficulties;
        public List<Espionage> espionages;
        public List<Era> eras;
        public List<Government> governments;
        public List<Building> buildings;
        public List<Race> races;
        public List<Citizen> citizens;
        public List<Trait> traits;
        public List<City> cities;
        public List<Event> events;
        public List<Overlay> overlays;
        public List<Terrain> terrains;
        public List<Experience> experiences;
    }
}
