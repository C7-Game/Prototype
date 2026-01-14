using System;
using System.Runtime.InteropServices;

namespace C7GameData {
	public class CitizenType {
		public ID Id;

		// Should this citizen be the default?
		public bool IsDefaultCitizen;

		// If !IsDefaultCitizen, the index of this specialist, for looking up in
		// popHeads.pcx. 0 is the first non-laborer row.
		public int SpecialistIndex;

		// Like "Laborer" or "Scientist"
		public string SingularName;

		public string CivilopediaEntry;

		// Like "Laborers" or "Scientists"
		public string PluralName;

		// If non-null, the tech needed to use this citizen type.
		public ID PrerequisiteTech;

		// The contribution, in gold per turn, that this citizen makes towards
		// luxuries/happiness.
		public int Luxuries;

		// The contribution, in beakers, that this citizen makes towards teching
		public int Research;

		// The contribution, in gold per turn, that this citizen makes towards
		// the treasury.
		public int Taxes;

		// TODO: Figure out the details of how corruption is reduced by
		// policemen.
		public int Corruption;

		// The contribution, in shields per turn, that this citizen makes
		// towards production.
		public int Construction;
	}
}
