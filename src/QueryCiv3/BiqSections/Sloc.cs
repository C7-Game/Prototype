using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct SLOC {
		public int Length;
		public int OwnerType; // 0: None, 1: Barbarian, 2: Civ, 3: Player
		public int Owner; // Race ID
		public int X;
		public int Y;
	}
}
