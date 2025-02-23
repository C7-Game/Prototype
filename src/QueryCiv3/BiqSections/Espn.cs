using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct ESPN {
		public int Length;

		private fixed byte Text[224];
		public string Description { get => Util.GetString(ref this, 4, 128); }
		public string Name { get => Util.GetString(ref this, 132, 64); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 196, 32); }

		private fixed byte Flags[4];
		public bool Diplomat { get => Util.GetFlag(Flags[0], 0); }
		public bool Spy { get => Util.GetFlag(Flags[0], 1); }

		public int BaseCost;
	}
}
