using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CTZN {
		public int Length;
		public int DefaultCitizen;

		private fixed byte Text[96];
		public string SingularName { get => Util.GetString(ref this, 8, 32); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 40, 32); }
		public string PluralName { get => Util.GetString(ref this, 72, 32); }

		public int Prerequisite;
		public int Luxuries;
		public int Research;
		public int Taxes;
		public int Corruption;
		public int Construction;
	}
}
