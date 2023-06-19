using System;
using System.IO;
using QueryCiv3;
using C7GameData;
using C7GameData.Save;

namespace BuildDevSave {
	class Program {

		static string GetCiv3Path { get => Civ3Location.GetCiv3Path(); }
		static string C7DefaultSaveDir { get => @"../../C7/Text"; }

		static void Main(string[] args) {
			if (args.Length < 1) {
				Console.WriteLine("provide civ3 SAV relative path as command line argument");
				return;
			}
			string saveFilePath = args[0];
			string fullSavePath = saveFilePath; //Path.Join(GetCiv3Path, saveFilePath);
			string outputName = Path.GetFileName(fullSavePath);
			outputName = outputName.Replace(Path.GetExtension(outputName), ".json");
			string outputPath = Path.Combine(C7DefaultSaveDir, "c7-static-map-save.json");

			byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(GetCiv3Path + @"/Conquests/conquests.biq");
			SavData mapReader = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(fullSavePath), defaultBicBytes);

			SaveGame output = ImportCiv3.ImportSav(fullSavePath, GetCiv3Path + @"/Conquests/conquests.biq");
			output.Save(outputPath);
		}
	}
}
