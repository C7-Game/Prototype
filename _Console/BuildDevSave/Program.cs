using System;
using System.IO;
using QueryCiv3;
using C7GameData;

namespace BuildDevSave {
	class Program {

		static string GetCiv3Path { get => Civ3Location.GetCiv3Path(); }
		static string C7DefaultSavePath { get => @"../../C7/Text/c7-static-map-save.json"; }

		static void Main(string[] args) {
			if (args.Length < 1) {
				Console.Out.WriteLine("provide civ3 SAV relative path as command line argument");
				return;
			}
			string saveFilePath = args[0];
			string fullSavePath = Path.Join(GetCiv3Path, saveFilePath);
			byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(GetCiv3Path + @"/Conquests/conquests.biq");
			SavData mapReader = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(fullSavePath), defaultBicBytes);

			C7SaveFormat output = ImportCiv3.ImportSav(fullSavePath, GetCiv3Path + @"/Conquests/conquests.biq");

			C7SaveFormat.Save(output, C7DefaultSavePath);
		}
	}
}
