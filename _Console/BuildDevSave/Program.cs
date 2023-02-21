using System;
using System.IO;
using QueryCiv3;
using C7GameData;

namespace BuildDevSave {
	class Program {

		static string GetCiv3Path { get => Civ3Location.GetCiv3Path(); }
		static string C7DefaultSaveDir { get => @"../../C7/Text/"; }

		static void Main(string[] args) {
			if (args.Length < 2) {
				Console.Out.WriteLine("provide civ3 SAV relative path and output file name as command line arguments");
				return;
			}
			string saveFilePath = args[0];
			string outputFileName = args[1];
			string fullSavePath = Path.Join(GetCiv3Path, saveFilePath);
			byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(GetCiv3Path + @"/Conquests/conquests.biq");
			SavData mapReader = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(fullSavePath), defaultBicBytes);

			C7SaveFormat output = ImportCiv3.ImportSav(fullSavePath, GetCiv3Path + @"/Conquests/conquests.biq");
			string outputFilePath = Path.Join(C7DefaultSaveDir, outputFileName);

			C7SaveFormat.Save(output, outputFilePath);
		}
	}
}
