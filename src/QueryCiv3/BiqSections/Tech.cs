using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct TECH {
		public int Length;

		private fixed byte Text[64];
		public string Name { get => Util.GetString(ref this, 4, 32); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 36, 32); }

		public int Cost;
		public int Era;
		public int AdvanceIcon;
		public int X;
		public int Y;
		public int Prerequisite1;
		public int Prerequisite2;
		public int Prerequisite3;
		public int Prerequisite4;

		private fixed byte Flags[4];
		public bool EnablesDiplomats { get => Util.GetFlag(Flags[0], 0); }
		public bool EnablesIrrigationEverywhere { get => Util.GetFlag(Flags[0], 1); }
		public bool EnablesBridges { get => Util.GetFlag(Flags[0], 2); }
		public bool DisablesDiseases { get => Util.GetFlag(Flags[0], 3); }
		public bool EnablesConscription { get => Util.GetFlag(Flags[0], 4); }
		public bool EnablesMobilization { get => Util.GetFlag(Flags[0], 5); }
		public bool EnablesRecycling { get => Util.GetFlag(Flags[0], 6); }
		public bool EnablesPrecisionBombing { get => Util.GetFlag(Flags[0], 7); }
		public bool EnablesMutualProtection { get => Util.GetFlag(Flags[1], 0); }
		public bool EnablesRightOfPassage { get => Util.GetFlag(Flags[1], 1); }
		public bool EnablesMilitaryAlliances { get => Util.GetFlag(Flags[1], 2); }
		public bool EnablesTradeEmbargoes { get => Util.GetFlag(Flags[1], 3); }
		public bool DoublesWealthProduction { get => Util.GetFlag(Flags[1], 4); }
		public bool EnablesTradeOverSea { get => Util.GetFlag(Flags[1], 5); }
		public bool EnablesTradeOverOcean { get => Util.GetFlag(Flags[1], 6); }
		public bool EnablesMapTrading { get => Util.GetFlag(Flags[1], 7); }
		public bool EnablesCommunicationTrading { get => Util.GetFlag(Flags[2], 0); }
		public bool NotRequiredForEraAdvancement { get => Util.GetFlag(Flags[2], 1); }
		public bool DoublesWorkerRate { get => Util.GetFlag(Flags[2], 2); }

		public int Flavors;
		private fixed byte UnknownBuffer[4];
	}
}
