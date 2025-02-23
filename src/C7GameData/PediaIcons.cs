
using System;
using System.Collections.Generic;
using System.IO;
using Serilog;

namespace C7GameData {

	// A class for interacting with the PediaIcons.txt file used in scenarios.
	class PediaIcons {
		private static readonly ILogger log = Log.ForContext<PediaIcons>();

		// A mapping from the civilopedia entry name (like PRTO_Spearman or
		// PRTO_Legionary_II) to the name used in the art directory (Spearman
		// and Legionary)
		private readonly Dictionary<string, string> unitArtMapping = new();

		// A mapping from the civilopedia entry name (like TECH_Map_Making) to
		// the small icon used in the science advisor display.
		private readonly Dictionary<string, string> techSmallIconMapping = new();

		private readonly string pediaIconsPath;

		public PediaIcons(string path) {
			pediaIconsPath = path;
			string[] lines = File.ReadAllLines(path);

			string animNamePrefix = "#ANIMNAME_";
			for (int i = 0; i < lines.Length - 1; ++i) {
				if (lines[i].StartsWith(animNamePrefix)) {
					string civilopediaName = lines[i].Substring(animNamePrefix.Length);
					if (civilopediaName.Contains("_ERAS_")) {
						// HACK: The civilopedia name for leaders differs by era, but the current
						// approach for resolving the artwork is era independent. Given a line like
						// #ANIMNAME_PRTO_Leader_ERAS_Ancient_Times we want to end up with
						// PRTO_Leader to match the biq file.
						civilopediaName = civilopediaName.Substring(0, civilopediaName.IndexOf("_ERAS_"));
					}

					string artName = lines[i + 1];
					unitArtMapping[civilopediaName] = artName;
					continue;
				}

				if (lines[i].StartsWith("#TECH") && !lines[i].EndsWith("_LARGE")) {
					// Drop the # from the line to get the civilopedia name
					// and then the next line is the icon path.
					techSmallIconMapping[lines[i].Substring(1)] = lines[i + 1];
				}
			}
		}

		public string GetTechIconPath(string civilopediaEntry) {
			return techSmallIconMapping[civilopediaEntry];
		}

		public string GetUnitArtName(string civilopediaEntry) {
			string artName = unitArtMapping[civilopediaEntry];
			if (artName == null) {
				log.Error($"Could not find #ANIMNAME_{civilopediaEntry} in PediaIcons file '{pediaIconsPath}");
				return "Warrior";
			}
			return artName;
		}
	}

}
