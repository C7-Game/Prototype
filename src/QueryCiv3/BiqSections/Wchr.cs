using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct WCHR {
		public int Length;
		public int SelectedClimate; // 0: Arid, 1: Normal, 2: Wet, 3: Random
		public int ActualClimate;
		public int SelectedBarbarianActivity; // -1: None, 0: Sedentary, 1: Roaming, 2: Restless, 3: Raging, 4: Random
		public int ActualBarbarianActivity;
		public int SelectedLandform; // 0: Archipelago, 1: Continents, 2: Pangaea, 3: Random
		public int ActualLandform;
		public int SelectedOceanCoverage; // 0: 80%, 1: 70%, 2: 60%, 3: Random
		public int ActualOceanCoverage;
		public int SelectedTemperature; // 0: Cool, 1: Temperate, 2: Warm, 3: Random
		public int ActualTemperature;
		public int SelectedAge; // 0: 3 billion years, 1: 4'', 2: 5'', 3: Random
		public int ActualAge;
		public int WorldSize;
	}
}
