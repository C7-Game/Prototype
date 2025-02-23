using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct UNIT {
		private fixed byte HeaderText[4];
		public int Length;
		public int ID;
		public int X;
		public int Y;
		public int PreviousX;
		public int PreviousY;
		public int OwnerID;
		public int Nationality;
		public int BarbTribe;
		public int UnitType;
		public int ExperienceLevel;

		// TODO: Fully populate all flags
		private fixed byte Flags[4];
		public bool AttackedThisTurn { get => Util.GetFlag(Flags[0], 2); }
		public bool GeneratedLeader { get => Util.GetFlag(Flags[0], 5); }

		public int Damage;
		public int MovementUsed; // In thirds of a point

		// The amount of progress the worker has made towards its job.
		//
		// This goes up by 2 each turn for a native worker, and 1 each turn for
		// a foreign worker (configurable via PRTO::WorkerStrength).
		public int WorkerProgressTowardsJob;
		public int WorkerJob; // Index into TFRM, -1 if idle
		private fixed byte UnknownBuffer2[4];
		public int LoadedOnUnitId;

		private fixed byte Flags2[12];
		public bool Fortified { get => Util.GetFlag(Flags2[0], 0); }

		// Note: this doesn't appear to correlate with whether a worker is
		// performing an action - use `WorkerJob != -1` for that.
		public bool Working { get => Util.GetFlag(Flags2[0], 3); }
		public bool GoTo { get => Util.GetFlag(Flags2[0], 4); }

		public int UseName;

		private fixed byte Text[60];
		public string Name { get => Util.GetString(ref this, 92, 60); }

		public int GoToX;
		public int GoToY;
		private fixed byte UnknownBuffer3[265];

		// if c3c:
		private fixed byte UnknownBuffer4[44];

		private fixed byte Flags3[4];
		public bool HasIDLSSection { get => Util.GetFlag(Flags3[3], 1); }

		private fixed byte UnknownBuffer5[7];
	}
}
