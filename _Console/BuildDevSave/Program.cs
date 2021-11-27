using System;
using System.IO;
using System.Text.Json;
using QueryCiv3;
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
            byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(GetCiv3Path + @"/Conquests/conquests.biq");
    		SavData mapReader = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(GetCiv3Path + SavFilePath), defaultBicBytes);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                // Lower-case the first letter in JSON because JSON naming standards
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // Pretty print because...well just because, OK?
                WriteIndented = true,
                // By default it only serializes getters, this makes it serialize fields, too
                IncludeFields = true,
            };

            C7SaveFormat output = ImportCiv3.ImportSav(GetCiv3Path + SavFilePath, GetCiv3Path + @"/Conquests/conquests.biq");
            output.players.Add(new Player(-1));
            output.players.Add(new Player(0x4040FFFF));

            string json = JsonSerializer.Serialize(output, jsonOptions);
            Console.WriteLine(json);
        }
    }
}
