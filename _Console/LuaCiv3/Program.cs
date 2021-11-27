using System;
using System.IO;
using System.Text.Json;
using QueryCiv3;
using MoonSharp.Interpreter;
using C7GameData;

namespace LuaCiv3
{
    class Program
    {
        // temp hack since I seem to have moved this out of QueryCiv3
        static string GetCiv3Path { get => @"/Users/jim/civ3"; }
        // also hack because can't be bothered to make a parameter
        static string SavFilePath { get => @"/Conquests/Saves/for-c7-seed-1234567.SAV"; }
        static void Main(string[] args)
        {

            RegisterQueryCiv3Types();
            Script lua = new Script();
            string runMe = File.ReadAllText("civ3map.lua");

            DynValue _ = lua.DoString(runMe);

            byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(GetCiv3Path + @"/Conquests/conquests.biq");
    		SavData mapReader = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(GetCiv3Path + SavFilePath), defaultBicBytes);

            _ = lua.Call(lua.Globals["process_sav"], mapReader);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                // Lower-case the first letter in JSON because JSON naming standards
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // Pretty print because...well just because, OK?
                WriteIndented = true
            };
            GameStateClass gameState = new GameStateClass
            {
                // refactoring, and this breaks
                // MapTiles = mapReader.Tile,
                MapWidth = mapReader.Wrld.Width,
                MapHeight = mapReader.Wrld.Height,
            };
            C7SaveFormat output = new C7SaveFormat
            {
                Version = "Mock save for hard-coded map data",
                GameState = gameState,
            };
            output = ImportCiv3.ImportSav(GetCiv3Path + SavFilePath, GetCiv3Path + @"/Conquests/conquests.biq");
            string foo = JsonSerializer.Serialize(output, jsonOptions);
            Console.WriteLine(foo);
        }

        // Enables these type instances to be accessed directly by Lua
        static void RegisterQueryCiv3Types() {
            UserData.RegisterType<SavData>();
            UserData.RegisterType<BicData>();
            UserData.RegisterType<Civ3File>();
            UserData.RegisterType<GameSection>();
            UserData.RegisterType<WrldSection>();
            UserData.RegisterType<MapTile>();
            UserData.RegisterType<ContItem>();
            UserData.RegisterType<LeaderItem>();
            UserData.RegisterType<CityItem>();
            
            UserData.RegisterType<BldgSection>();
            UserData.RegisterType<CtznSection>();
            UserData.RegisterType<CultSection>();
            UserData.RegisterType<DiffSection>();
            UserData.RegisterType<ErasSection>();
            UserData.RegisterType<EspnSection>();
            UserData.RegisterType<ExprSection>();
            UserData.RegisterType<GoodSection>();
            UserData.RegisterType<GovtSection>();
            UserData.RegisterType<PrtoSection>();
            UserData.RegisterType<RaceSection>();
            UserData.RegisterType<TechSection>();
            UserData.RegisterType<TfrmSection>();
            UserData.RegisterType<TerrSection>();
            UserData.RegisterType<WsizSection>();
            UserData.RegisterType<FlavSection>();
        }
    }
}
