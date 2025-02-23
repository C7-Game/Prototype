using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct POPD {
		private fixed byte HeaderText[4];
		public int Length;
		public int SpecialistCount;
		public int CitizenCount;
	}
}
