using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CLNY {
		public int Length;
		public int OwnerType;
		public int Owner;
		public int X;
		public int Y;
		public int ImprovementType;
	}
}
