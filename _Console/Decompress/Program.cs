using System;
using System.IO;
using Blast;

class Program {

	private static void DecompressFile(string inputFile, string outputFile) {
		using (FileStream input = new FileStream(inputFile, FileMode.Open)) {
			using (FileStream output = new FileStream(outputFile, FileMode.Create)) {
				new BlastDecoder(input, output).Decompress();
			}
		}
	}

	static void Main(string[] args) {
		if (args.Length < 2) {
			Console.WriteLine("provide args <input file> <output file>");
			Environment.Exit(1);
		}
		string inputFile = args[0];
		string outputFile = args[1];
		try {
			DecompressFile(inputFile, outputFile);
			Console.WriteLine($"decompressed {inputFile} to {outputFile}");
		} catch (BlastException bex) {
			Console.WriteLine($"blast decompression failed with exception: {bex.Message}");
		}
	}
}
