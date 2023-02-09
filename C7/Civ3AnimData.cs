
// Civ3AnimData's purpose is to store the data associated with Civ 3 animations, for example the contents of each folder in Art/Units. It does lazy
// loading & memoization so each file is loaded only when it's needed and then stored so it is only ever loaded once per game. The main (and only)
// instance of Civ3AnimData is kept in Game, AnimationTracker holds a reference to it.

// It would be nice to load this data close to where it's used, e.g., have UnitLayer load the FlicSheets, instead of putting all the loading code in
// one detached class like this. That's how things originally worked but I created Civ3AnimData to solve two issues:
// 1. AnimationTracker and UnitLayer both need to load the unit INIs. So we either have a common place to store the INIs or duplication of work, and I
// think the former is the better choice.
// 2. AnimationTracker needs to know the duration of animations, which awkwardly cannot be determined based on the INI files alone. In order to know
// the duration of an anim you must know how many frames it has, and the only way to know that is to read its flic file.

// The intended usage is to access the animation data through a Civ3Anim object obtained through the "forUnit" or "forEffect" methods. For example:
//   civ3AnimData.forUnit("Warrior", MapUnit.AnimatedAction.FORTIFY).playSound()
// To play the warrior's foritfy sound effect.

using System;
using System.Collections.Generic;
using Godot;
using IniParser;
using IniParser.Model;
using C7GameData;

public partial class Civ3AnimData
{
	private AudioStreamPlayer audioPlayer;

	public Civ3AnimData(AudioStreamPlayer audioPlayer)
	{
		this.audioPlayer = audioPlayer;
	}

	private Dictionary<string, IniData> iniDatas = new Dictionary<string, IniData>();

	public IniData getINIData(string pathKey)
	{
		IniData tr;
		if (! iniDatas.TryGetValue(pathKey, out tr)) {
			string fullPath = Util.Civ3MediaPath(pathKey);
			tr = (new FileIniDataParser()).ReadFile(fullPath);
			iniDatas.Add(pathKey, tr);
		}
		return tr;
	}

	public IniData getUnitINIData(string unitTypeName)
	{
		return getINIData(String.Format("Art/Units/{0}/{0}.INI", unitTypeName));
	}

	// Looks up the name of the flic file associated with a given action in an animation INI. If there is no flic file listed for the action,
	// returns instead the file name for the default action, and if that's missing too, throws an exception.
	public string getFlicFileName(IniData iniData, MapUnit.AnimatedAction action)
	{
		string fileName = iniData["Animations"][action.ToString()];
		if ((fileName != null) && (fileName != ""))
			return fileName;
		else if (action != MapUnit.AnimatedAction.DEFAULT)
			return getFlicFileName(iniData, MapUnit.AnimatedAction.DEFAULT);
		else
			throw new Exception("Missing default animation"); // TODO: Add the INI's file name to the error message
	}

	private Dictionary<string, Util.FlicSheet> flicSheets = new Dictionary<string, Util.FlicSheet>();

	public Util.FlicSheet getFlicSheet(string rootPath, IniData iniData, MapUnit.AnimatedAction action)
	{
		Util.FlicSheet tr;
		string pathKey = rootPath + "/" + getFlicFileName(iniData, action);
		if (! flicSheets.TryGetValue(pathKey, out tr)) {
			(tr, _) = Util.loadFlicSheet(pathKey);
			flicSheets.Add(pathKey, tr);
		}
		return tr;
	}

	private Dictionary<string, AudioStreamWav> wavs = new Dictionary<string, AudioStreamWav>();

	public void playSound(string rootPath, IniData iniData, MapUnit.AnimatedAction action)
	{
		string fileName = iniData["Sound Effects"][action.ToString()];
		if (fileName.EndsWith(".WAV", StringComparison.CurrentCultureIgnoreCase)) {
			AudioStreamWav wav;
			var pathKey = rootPath + "/" + fileName;
			if (! wavs.TryGetValue(pathKey, out wav)) {
				wav = Util.LoadWAVFromDisk(Util.Civ3MediaPath(pathKey));
				wavs.Add(pathKey, wav);
			}
			audioPlayer.Stream = wav;
			audioPlayer.Play();
		}
	}

	public Civ3Anim forUnit(string unitTypeName, MapUnit.AnimatedAction action)
	{
		return new Civ3Anim(this, unitTypeName, action);
	}

	public Civ3Anim forEffect(AnimatedEffect effect)
	{
		return new Civ3Anim(this, effect);
	}
}

public partial class Civ3Anim
{
	public Civ3AnimData civ3AnimData  { get; private set; }
	public string folderPath { get; private set; } // For example "Art/Units/Warrior" or "Art/Animations/Trajectory"
	public string iniFileName { get; private set; }
	public MapUnit.AnimatedAction action { get; private set; }

	public Civ3Anim(Civ3AnimData civ3AnimData, string unitTypeName, MapUnit.AnimatedAction action)
	{
		this.civ3AnimData = civ3AnimData;
		this.folderPath = "Art/Units/" + unitTypeName;
		this.iniFileName = unitTypeName + ".ini";
		this.action = action;
	}

	public static readonly Dictionary<AnimatedEffect, string> effectCategories = new Dictionary<AnimatedEffect, string>
	{
		{ AnimatedEffect.Hit      , "Trajectory" },
		{ AnimatedEffect.Hit2     , "Trajectory" },
		{ AnimatedEffect.Hit3     , "Trajectory" },
		{ AnimatedEffect.Hit5     , "Trajectory" },
		{ AnimatedEffect.Miss     , "Trajectory" },
		{ AnimatedEffect.WaterMiss, "Trajectory" }
	};

	public static readonly Dictionary<AnimatedEffect, string> effectINIFileNames = new Dictionary<AnimatedEffect, string>
	{
		{ AnimatedEffect.Hit      , "hit.ini" },
		{ AnimatedEffect.Hit2     , "hit2.ini" },
		{ AnimatedEffect.Hit3     , "hit3.ini" },
		{ AnimatedEffect.Hit5     , "hit5.ini" },
		{ AnimatedEffect.Miss     , "miss.ini" },
		{ AnimatedEffect.WaterMiss, "water miss.ini" }
	};

	public Civ3Anim(Civ3AnimData civ3AnimData, AnimatedEffect effect)
	{
		this.civ3AnimData = civ3AnimData;
		this.folderPath = "Art/Animations/" + effectCategories[effect];
		this.iniFileName = effectINIFileNames[effect];
		this.action = MapUnit.AnimatedAction.DEATH;
	}

	public IniData getINIData()
	{
		return civ3AnimData.getINIData(folderPath + "/" + iniFileName);
	}

	public Util.FlicSheet getFlicSheet()
	{
		return civ3AnimData.getFlicSheet(folderPath, getINIData(), action);
	}

	public void playSound()
	{
		civ3AnimData.playSound(folderPath, getINIData(), action);
	}

	public double getDuration()
	{
		Util.FlicSheet flicSheet = getFlicSheet();
		double frameCount = flicSheet.indices.GetWidth() / flicSheet.spriteWidth;
		return frameCount / 20.0; // Civ 3 anims often run at 20 FPS   TODO: Do they all? How could we tell? Is it exactly 20 FPS?
	}
}
