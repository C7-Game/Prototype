using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct FLAVFLAV {
		private fixed int Relationships[7];
		public int this[int index] { get => Relationships[index]; }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct FLAV {
		private fixed byte UnknownBuffer[4];

		private fixed byte Text[256];
		public string Name { get => Util.GetString(ref this, 4, 256); }

		// So FLAV should be considered a dynamic section, because it has an amount of flavor relationship data determined by NumberOfFlavors
		// However, NumberOfFlavors seems to always be 7, and it doesn't look like flavors can be added to or removed in the Civ3Editor
		// So for now, I'm treating FLAV as static until a counterexample is discovered
		public int NumberOfFlavors;
		public FLAVFLAV FlavorRelationship;
	}
}
