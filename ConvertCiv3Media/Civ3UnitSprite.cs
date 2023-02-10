using System;
using System.Collections.Generic;
using IniParser;
using IniParser.Model;

namespace ConvertCiv3Media {
	// Under construction

	// The order of direction animations in unit FLC files
	public enum Direction {
		SW,
		S,
		SE,
		E,
		NE,
		N,
		NW,
		W
	}
	public enum UnitAction {
		BLANK,
		DEFAULT,
		WALK,
		RUN,
		ATTACK1,
		ATTACK2,
		ATTACK3,
		DEFEND,
		DEATH,
		DEAD,
		FORTIFY,
		FORTIFYHOLD,
		FIDGET,
		VICTORY,
		TURNLEFT,
		TURNRIGHT,
		BUILD,
		ROAD,
		MINE,
		IRRIGATE,
		FORTRESS,
		CAPTURE,
		JUNGLE,
		FOREST,
		PLANT,
		STOP_AT_LAST_FRAME,
		// *** The following three are causing null filenames that don't match "" on non-warrior INIs
		// *** Oh, they don't exist in other INI's Animation sections!
		// PauseROAD,
		// PauseMINE,
		// PauseIRRIGATE

	}
	// will probably make these to-override methods in Civ3UnitSprite instead
	public interface ISprite {
		void Animation(UnitAction action, Direction direction);
		void Play();
		void Stop();
		// public void Move();
		// public void PlaySound(UnitAction action);
		// public void SetLocation(int x, int y);
	}
	public class Civ3UnitSprite {
		protected Flic[] Animations = new Flic[Enum.GetNames(typeof(UnitAction)).Length];
		// TODO: handle mismatched cases in ini file .. maybe try INI then ini ?
		// unitColor must be from 0 - 31
		public Civ3UnitSprite(Civ3UnitSprite civ3UnitSprite) {
		}

		public Civ3UnitSprite(string unitPath, byte unitColor = 0) {
			// TODO: Parameterize this and/or take ini path and chop it up
			// string UnitIniPath = unitPath + "Warrior.INI";
			FileIniDataParser UnitIniFile = new FileIniDataParser();
			IniData UnitIniData = UnitIniFile.ReadFile(unitPath);

			// TODO: Fix this total hack
			string[] foo = unitPath.Split(new char[]{'/','\\'});
			foo[foo.Length - 2] = "Palettes";
			foo[foo.Length - 1] = String.Format("ntp{0:D02}.pcx", unitColor);
			Pcx UnitPal = new Pcx(String.Join("/", foo));

			// TODO: Fix this total hack
			string[] bar = unitPath.Split(new char[]{'/','\\'});
			foreach (UnitAction actn in Enum.GetValues(typeof(UnitAction))) {
				if (UnitIniData["Animations"][actn.ToString()] != "" && UnitIniData["Animations"][actn.ToString()] != null) {
					bar[bar.Length - 1] = UnitIniData["Animations"][actn.ToString()];
					Flic UnitFlic = new Flic(String.Join("/", bar));

					byte[,] CivColorUnitPal = new byte[256,3];
					for (int i = 0, palSplit = 64; i < 256; i++) {
						byte[,] TempPal = i < palSplit ?  UnitPal.Palette : UnitFlic.Palette ;
						for (int j = 0; j < 3; j++) {
							CivColorUnitPal[i, j] = TempPal[i < palSplit ? i : i, j];
						}
					}
					// foreach(Direction dir in Enum.GetValues(typeof(Direction))) {
					UnitFlic.Palette = CivColorUnitPal;
					Animations[(int)actn] = UnitFlic;
					// }

				}
			}
		}

		// Returns all non-empty key, value pairs for the "Animations" section of a unit INI file. I'm copying this out of the Civ3UnitSprite
		// constructor for use by UnitLayer.
		public static Dictionary<string, string> getINIAnimationsInfo(string iniPath) {
			FileIniDataParser UnitIniFile = new FileIniDataParser();
			IniData UnitIniData = UnitIniFile.ReadFile(iniPath);
			var tr = new Dictionary<string, string>();
			foreach (UnitAction actn in Enum.GetValues(typeof(UnitAction))) {
				var fileName = UnitIniData["Animations"][actn.ToString()];
				if ((fileName != null) && (fileName != ""))
					tr[actn.ToString()] = fileName;
			}
			return tr;
		}

		/*
		public virtual void InitDisplay() {
			// override this method in the display framework to convert media to display framework objects
		}
		*/
		virtual public void Animation(UnitAction action, Direction direction) {
			// override this method in the display framework
		}
		/*
		virtual public void Play() {
			// override this method in the display framework to start animation
		}
		virtual public void Stop() {
			// override this method in the display framework to stop animation
		}
		*/
		virtual public void Move(Direction direction, float speed = 1) {
		}
		// public void PlaySound(UnitAction action);
	}
}
