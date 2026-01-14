using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace C7Engine.PalaceMinigame;

/*
  Parser and data models for the Civ3 Palace Minigame config file (PalaceView.txt).
  The config format description can be found on CivFanatics: https://forums.civfanatics.com/threads/palace-view-modifications-palaceview-txt.619788/post-14813780
*/

using TextureConfig = Dictionary<string, Culture>;

public class Culture {
	public string Name;
	public List<Building> Buildings = [];
	public ButtonTextures ButtonTextures = new();
}

public class Building {
	public int Index;
	public string TexturePath;
	public List<int> Prerequisites = []; // list of building indexes
	public int X;
	public int Y;
}

public class ButtonTextures {
	public string Normal;
	public string Hover;
	public string Pressed;
}

public class ConfigParser {
	private enum Section {
		None,
		Filenames,
		Rules,
		XCoords,
		YCoords,
		Comment,
		Icons,
	}

	private Section currentSection = Section.None;
	private string currentCulture = null;
	private int sectionLineCount = 0;
	private readonly TextureConfig config = new();
	private readonly List<int> xCoords = [];
	private readonly List<int> yCoords = [];

	public TextureConfig Parse(string filePath) {
		var lines = File.ReadAllLines(filePath)
				.Select(line => line.Split(';')[0].Trim()) // Remove comments
				.Where(line => !string.IsNullOrEmpty(line))
				.ToList();

		foreach (var line in lines) {
			if (DetectSectionChange(line)) {
				sectionLineCount = 0;
				continue;
			}

			HandleLine(line);
			sectionLineCount++;
		}

		AssignCoordinates();
		return config;
	}

	private bool DetectSectionChange(string line) {
		if (line.StartsWith("#PVFNAME_")) {
			currentCulture = ExtractCultureName(line);
			currentSection = Section.Filenames;
			config[currentCulture] = new() { Name = currentCulture };
			return true;
		}
		if (line.StartsWith("#PVRULES_")) {
			currentCulture = ExtractCultureName(line);
			currentSection = Section.Rules;
			return true;
		}
		if (line == "#PV_SPRITE_XLOCS") {
			currentSection = Section.XCoords;
			return true;
		}
		if (line == "#PV_SPRITE_YLOCS") {
			currentSection = Section.YCoords;
			return true;
		}
		if (line.StartsWith("#PVICONS_")) {
			currentCulture = ExtractCultureName(line);
			currentSection = Section.Icons;
			return true;
		}
		if (line == "#COMMENT") {
			currentSection = Section.Comment;
			return true;
		}
		return false;
	}

	private void HandleLine(string line) {
		switch (currentSection) {
			case Section.Filenames:
				if (sectionLineCount == 0) {
					break;
				} else {
					var building = new Building {
						Index = sectionLineCount - 1,
						TexturePath = ExpandTexturePath(line)
					};
					config[currentCulture].Buildings.Add(building);
				}
				break;

			case Section.Rules:
				var prerequisites = ParseRuleBits(line);
				config[currentCulture].Buildings[sectionLineCount].Prerequisites = prerequisites;
				break;

			case Section.XCoords:
				xCoords.Add(int.Parse(line));
				break;

			case Section.YCoords:
				yCoords.Add(int.Parse(line));
				break;

			case Section.Icons:
				var btn = config[currentCulture].ButtonTextures;
				var path = ExpandTexturePath(line);

				if (sectionLineCount == 0) {
					btn.Normal = path;
				} else if (sectionLineCount == 1) {
					btn.Hover = path;
				} else if (sectionLineCount == 2) {
					btn.Pressed = path;
				}
				break;
		}
	}

	private static string ExpandTexturePath(string path) {
		return Path.Combine("Art", "PalaceView", path);
	}

	private static string ExtractCultureName(string line) {
		return line.Split('_').Last().ToLowerInvariant();
	}

	private static List<int> ParseRuleBits(string ruleBits) {
		var prerequisites = new List<int>();
		for (int i = 0; i < ruleBits.Length; i++) {
			if (ruleBits[i] == '1') {
				prerequisites.Add(i);
			}
		}
		return prerequisites;
	}

	private void AssignCoordinates() {
		foreach (var culture in config.Values) {
			for (int i = 0; i < culture.Buildings.Count && i < xCoords.Count && i < yCoords.Count; i++) {
				culture.Buildings[i].X = xCoords[i];
				culture.Buildings[i].Y = yCoords[i];
			}
		}
	}
}
