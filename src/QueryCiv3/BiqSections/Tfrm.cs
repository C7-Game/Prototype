using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct TFRM {
		public int Length;

		private fixed byte Text[64];
		public string Name { get => Util.GetString(ref this, 4, 32); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 36, 32); }

		public int TurnsToComplete;
		public int Required;
		public int RequiredResource1;
		public int RequiredResource2;

		private fixed byte Text2[32];
		public string Order { get => Util.GetString(ref this, 84, 32); }
	}
}
