using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct ERAS {
		public int Length;

		private fixed byte Text[256];
		public string Name { get => Util.GetString(ref this, 4, 64); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 68, 32); }
		public string Researcher1 { get => Util.GetString(ref this, 100, 32); }
		public string Researcher2 { get => Util.GetString(ref this, 132, 32); }
		public string Researcher3 { get => Util.GetString(ref this, 164, 32); }
		public string Researcher4 { get => Util.GetString(ref this, 196, 32); }
		public string Researcher5 { get => Util.GetString(ref this, 228, 32); }

		public int NumberOfUsedResearcherNames;
		private fixed byte Unknown[4];
	}
}
