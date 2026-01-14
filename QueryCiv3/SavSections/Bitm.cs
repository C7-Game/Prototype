using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct BITM {
		private fixed byte HeaderText[4];
		public int Length;
		private fixed byte UsableBuildingBits[32];
		public int BuildingCount;
		public int BuildingBytes;

		public bool IsBuildingUsable(int buildingIndex) {
			int byteIndex = buildingIndex / 8;
			int bitIndex = buildingIndex % 8;
			fixed (byte* ptr = UsableBuildingBits) {
				return (ptr[byteIndex] & (1 << bitIndex)) != 0;
			}
		}
	}
}
