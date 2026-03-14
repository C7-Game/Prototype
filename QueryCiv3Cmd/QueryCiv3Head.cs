
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using QueryCiv3;

internal class Program {
	private static void Main(string[] args) {

		string outputFilename = "C:\\Program Files (x86)\\Atari\\Civilization III Complete\\Conquests\\Saves\\output.SAV";
		string filename = "C:\\Program Files (x86)\\Atari\\Civilization III Complete\\Conquests\\Saves\\output.SAV";
		string referenceFilename = "C:\\Program Files (x86)\\Atari\\Civilization III Complete\\Conquests\\Saves\\PBE-060-Mongols-000.SAV";

		string[] headers = {
			//"CIV3", //differs by noise
			//"GAME", //differs by noise
			//"CNSL",
			//"LEAD", //differs by noise
			//"RPLS",
			//"RPLT",
		};
		//BLDG seems to change even when nothing else does, which is wack

		string patchSectionName = "GAME";
		int patchIndex = 1;
		int patchOffset = 0x144;
		byte patchValue = 31;

		bool showDifference = true;
		bool patchingFromReferenceFile = false;
		bool patchingFromConsole = true;

		byte[] fileBytes = Util.ReadFile(filename);
		Civ3File file = new Civ3File(fileBytes);

		byte[] referenceFileBytes = Util.ReadFile(referenceFilename);
		Civ3File referenceFile = new Civ3File(referenceFileBytes);

		if (file.IsGameFile && referenceFile.IsGameFile) {

			OutputSummary(file);
			foreach (string header in headers) {
				DumpAllSections(filename, file, header);
				DumpAllSections(referenceFilename, referenceFile, header);
			}

			int matchingTo = 0;
			int referenceOffset = 0;
			for (int i = 0; i < referenceFile.Sections.Length && i < file.Sections.Length; i++) {

				if (matchingTo != 0) {
					if (file.Sections[i].Name == referenceFile.Sections[i].Name) {
						referenceOffset = 0;
						matchingTo = 0;
					} else if (file.Sections[i].Name == referenceFile.Sections[i + matchingTo].Name) {
						referenceOffset = matchingTo;
						matchingTo = 0;
					} else if (file.Sections[i + matchingTo].Name == referenceFile.Sections[i].Name) {
						referenceOffset = -matchingTo;
						matchingTo = 0;
						i += matchingTo;
					} else {
						matchingTo--;
						continue;
					}
				}

				Civ3Section section = file.Sections[i];
				Civ3Section referenceSection = referenceFile.Sections[i + referenceOffset];

				if (section.Name != referenceSection.Name) {
					if (matchingTo <= 0) {
						matchingTo--;
					} else {
						i += matchingTo;
						matchingTo = -1;
					}
					continue;
				}

				byte[] content = GetSectionFromIndex(file, i);
				byte[] referenceContent = GetSectionFromIndex(referenceFile, i + referenceOffset);

				if (!content.SequenceEqual(referenceContent) && headers.Contains(section.Name)) {

					Console.WriteLine("{0} ({1}) differs", section.Name, referenceSection.Name);

					if (showDifference) {
						for (int j = 0; j < content.Length || j < referenceContent.Length; j++) {
							bool validInReference = j + referenceOffset < referenceContent.Length && j + referenceOffset >= 0;
							Console.WriteLine("{0} / {1}", j >= content.Length ? " " : content[j], validInReference ? referenceContent[j + referenceOffset] : " ");
						}
					}

					if (patchingFromReferenceFile) {
						ApplyPatch(fileBytes, referenceContent, referenceSection.Offset);
					}

				} else {

					//Console.WriteLine("{0} matches", section.Name);

				}
				//get both sections
				//compare the difference
			}

			//TODO output Blast encrypted files
			if (patchingFromReferenceFile || patchingFromConsole) {

				Civ3Section? patchSection = GetSectionOfType(file, patchSectionName, patchIndex);
				if (patchSection != null) {
					ApplyPatch(fileBytes, [patchValue], patchSection.Offset + patchOffset);
				} else {
					Console.WriteLine("Could not apply patch: no section found at {0} {1}", patchSectionName, patchIndex);
				}

				File.WriteAllBytes(outputFilename, fileBytes);

			}


		} else {
			Console.WriteLine("Not a game file");
			Environment.Exit(1);
		}

	}

	private static void DumpAllSections(string filename, Civ3File file, string header) {
		bool matchFound = true;
		int index = 0;
		while (matchFound) {
			byte[] bytesFound = GetRegionOfType(file, header, index);
			if (bytesFound.Length == 0) {
				matchFound = false;
			} else {
				File.WriteAllBytes(filename + " " + header + index + ".bin", bytesFound);
				index++;
			}
		}
	}

	private static void ApplyPatch(byte[] fileBytes, byte[] patch, int offset) {
		for (int i = 0; i < patch.Length; i++) {
			fileBytes[i + offset] = patch[i];
		}
	}

	//a bit of copy pasting never hurt nobody.
	private static Civ3Section? GetSectionOfType(Civ3File file, string header, int ignore = 0) {

		foreach (Civ3Section section in file.Sections) {

			if (section.Name == header) {
				if (ignore > 0) {
					ignore--;
					continue;
				}
				return section;
			}
		}

		return null;

	}

	private static byte[] GetRegionOfType(Civ3File file, string header, int ignore = 0) {
		Civ3Section? selection = null;
		Civ3Section? selectionEnd = null;

		foreach (Civ3Section section in file.Sections) {

			if (selection != null) {
				selectionEnd = section;
				break;
			}

			if (section.Name == header) {
				if (ignore > 0) {
					ignore--;
					continue;
				}
				selection = section;
			}
		}

		if (selection == null) {
			return Array.Empty<byte>();
		} else {
			return GetSection(file, selection, selectionEnd);
		}
	}

	private static byte[] GetSectionFromIndex(Civ3File file, int i) {
		Civ3Section section = file.Sections[i];
		Civ3Section? terminator = i != file.Sections.Length - 1 ? file.Sections[i + 1] : null;

		return GetSection(file, section, terminator);
	}

	private static byte[] GetSection(Civ3File file, Civ3Section start, Civ3Section? terminator) {
		if (terminator == null) {
			return file.GetBytes(start.Offset, file.Length - start.Offset);
		} else {
			return file.GetBytes(start.Offset, terminator.Offset - start.Offset);
		}
	}

	private static void OutputSummary(Civ3File file) {
		Dictionary<string, int> firstSections = new Dictionary<string, int>();

		//TODO get sections
		foreach (Civ3Section section in file.Sections) {

			var contained = false;

			foreach (KeyValuePair<string, int> section2 in firstSections) {
				if (section2.Key == section.Name) {
					contained = true;
					firstSections.Remove(section2.Key);
					firstSections.Add(section2.Key, section2.Value + 1);
					break;
				}
			}

			if (contained) {
				continue;
			}

			firstSections.Add(section.Name, 1);

		}

		foreach (KeyValuePair<string, int> section in firstSections) {
			Console.Write("{0} {1}; ", section.Key, section.Value);
		}
		Console.WriteLine();
	}
}
