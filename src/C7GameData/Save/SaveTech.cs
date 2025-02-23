using System.Collections.Generic;

namespace C7GameData.Save {
	// A class representing a single technology that can be researched.
	//
	// This is intended for use in save games, so it does not have recursive
	// references for prereqs.
	public class SaveTech {
		public ID id;
		public string Name { get; set; }
		public string CivilopediaEntry { get; set; }
		public int Cost;

		// The civilopedia name of the era this tech is part of
		// (like ERA_Ancient_Times). This is what art lookups are based on.
		public string EraCivilopediaName { get; set; }

		// The path, like "Art\tech chooser\Icons\39-Mapmaking-small.pcx", of
		// the small icon for this tech.
		public string SmallIconPath;

		// The position of this tech within the tech advisor UI.
		public int X;
		public int Y;

		public List<ID> Prerequisites = new();

		public C7GameData.Tech ToTechWithoutPrereqs() {
			C7GameData.Tech result = new() {
				id = this.id,
				Name = this.Name,
				CivilopediaEntry = this.CivilopediaEntry,
				Cost = this.Cost,
				EraCivilopediaName = this.EraCivilopediaName,
				SmallIconPath = this.SmallIconPath,
				X = this.X,
				Y = this.Y
			};
			return result;
		}
	}

}
