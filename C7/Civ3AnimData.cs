
// Civ3AnimData's purpose is to store the data associated with Civ 3 animations, for example the contents of each folder in Art/Units. It does lazy
// loading & memoization so each file is loaded only when it's needed and then stored so it is only ever loaded once per game. The main (and only)
// instance of Civ3AnimData is kept in Game, AnimationTracker holds a reference to it.

// It would be nice to load this data close to where it's used, e.g., have UnitLayer load the FlicSheets, instead of putting all the loading code in
// one detached class like this. That's how things originally worked but I created Civ3AnimData to solve two issues:
// 1. AnimationTracker and UnitLayer both need to load the unit INIs. So we either have a common place to store the INIs or duplication of work, and I
// think the former is the better choice.
// 2. AnimationTracker needs to know the duration of animations, which awkwardly cannot be determined based on the INI files alone. In order to know
// the duration of an anim you must know how many frames it has, and the only way to know that is to read its flic file.

using System;
using System.Collections.Generic;
using Godot;
using IniParser;
using IniParser.Model;
using C7GameData;

public class Civ3AnimData
{
	private AudioStreamPlayer audioPlayer;

	public Civ3AnimData(AudioStreamPlayer audioPlayer)
	{
		this.audioPlayer = audioPlayer;
	}





	private Dictionary<string, IniData> iniDatas = new Dictionary<string, IniData>();

	private IniData getINIData(string pathKey)
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

	public IniData getEffectINIData(AnimatedEffect effect)
	{
		string path;
		switch (effect) {
		case AnimatedEffect.Hit:       path = "Art/Animations/Trajectory/hit.ini"       ; break;
		case AnimatedEffect.Hit2:      path = "Art/Animations/Trajectory/hit2.ini"      ; break;
		case AnimatedEffect.Hit3:      path = "Art/Animations/Trajectory/hit3.ini"      ; break;
		case AnimatedEffect.Hit5:      path = "Art/Animations/Trajectory/hit5.ini"      ; break;
		case AnimatedEffect.Miss:      path = "Art/Animations/Trajectory/miss.ini"      ; break;
		case AnimatedEffect.WaterMiss: path = "Art/Animations/Trajectory/water miss.ini"; break;
		default:
			throw new System.Exception ("Must provide path to INI file for effect: " + effect.ToString());
		}
		return getINIData(path);
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

	public Util.FlicSheet getFlicSheet(string unitTypeName, MapUnit.AnimatedAction action)
	{
		return getFlicSheet("Art/Units/" + unitTypeName, getUnitINIData(unitTypeName), action);
	}

	public Util.FlicSheet getFlicSheet(AnimatedEffect effect)
	{
		// TODO: Refactor this along with the paths in getEffectINIData
		string category;
		switch (effect) {
		case AnimatedEffect.Hit:       category = "Trajectory"; break;
		case AnimatedEffect.Hit2:      category = "Trajectory"; break;
		case AnimatedEffect.Hit3:      category = "Trajectory"; break;
		case AnimatedEffect.Hit5:      category = "Trajectory"; break;
		case AnimatedEffect.Miss:      category = "Trajectory"; break;
		case AnimatedEffect.WaterMiss: category = "Trajectory"; break;
		default:
			throw new System.Exception ("Must provide category for effect: " + effect.ToString());
		}
		return getFlicSheet("Art/Animations/" + category, getEffectINIData(effect), MapUnit.AnimatedAction.DEATH);
	}





	private Dictionary<string, AudioStreamSample> wavs = new Dictionary<string, AudioStreamSample>();

	public void playSound(string unitTypeName, MapUnit.AnimatedAction action)
	{
		string fileName = getUnitINIData(unitTypeName)["Sound Effects"][action.ToString()];
		if (fileName.EndsWith(".WAV", StringComparison.CurrentCultureIgnoreCase)) {
			AudioStreamSample wav;
			var key = String.Format("{0}.{1}", unitTypeName, action.ToString());
			if (! wavs.TryGetValue(key, out wav)) {
				wav = Util.LoadWAVFromDisk(Util.Civ3MediaPath(String.Format("Art/Units/{0}/{1}", unitTypeName, fileName)));
				wavs.Add(key, wav);
			}
			audioPlayer.Stream = wav;
			audioPlayer.Play();
		}
	}

	public void playSound(AnimatedEffect effect)
	{
		// TODO: Implement this
	}





	public double getDuration(Util.FlicSheet flicSheet)
	{
		double frameCount = flicSheet.indices.GetWidth() / flicSheet.spriteWidth;
		return frameCount / 20.0; // Civ 3 anims often run at 20 FPS   TODO: Do they all? How could we tell? Is it exactly 20 FPS?
	}

	public double getDuration(string unitTypeName, MapUnit.AnimatedAction action)
	{
		return getDuration(getFlicSheet(unitTypeName, action));
	}

	public double getDuration(AnimatedEffect effect)
	{
		return getDuration(getFlicSheet(effect));
	}
}
