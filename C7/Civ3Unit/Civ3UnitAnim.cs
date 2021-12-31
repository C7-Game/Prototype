
// Civ3UnitAnim's purpose is to store the data associated with Civ 3 unit animations, meaning the contents of the folders in Art\Units. It does lazy
// loading & memoization so each file is loaded only when it's needed and then stored so it is only ever loaded once per game. The main (and only)
// instance of Civ3UnitAnim is kept in Game, AnimationTracker holds a reference to it.

// It would be nice to load this data close to where it's used, e.g., have UnitLayer load the FlicSheets, instead of putting all the loading code in
// one detached class like this. That's how things originally worked but I created Civ3UnitAnim to solve two issues:
// 1. AnimationTracker and UnitLayer both need to load the unit INIs. So we either have a common place to store the INIs or duplication of work, and I
// think the former is the better choice.
// 2. AnimationTracker needs to know the duration of animations, which awkwardly cannot be determined based on the INI files alone. In order to know
// the duration of an anim you must know how many frames it has, and the only way to know that is to read its flic file.

using System;
using System.Collections.Generic;
using IniParser;
using IniParser.Model;
using C7GameData;

public class Civ3UnitAnim
{
	private Dictionary<string, IniData> unitIniDatas = new Dictionary<string, IniData>();

	public IniData getUnitINIData(string unitTypeName)
	{
		IniData tr;
		if (! unitIniDatas.TryGetValue(unitTypeName, out tr)) {
			string iniPath = Util.Civ3MediaPath(String.Format("Art/Units/{0}/{0}.INI", unitTypeName));
			tr = (new FileIniDataParser()).ReadFile(iniPath);
			unitIniDatas.Add(unitTypeName, tr);
		}
		return tr;
	}

	// Returns the name of the flic file for an action by a unit type, read from that unit type's INI file. If there is no flic file listed for
	// the given action, returns instead the file name for the default action, and if that's missing too, throws an exception.
	public string getFlicFileName(string unitTypeName, MapUnit.AnimatedAction action)
	{
		string fileName = getUnitINIData(unitTypeName)["Animations"][action.ToString()];
		if ((fileName != null) && (fileName != ""))
			return fileName;
		else if (action != MapUnit.AnimatedAction.DEFAULT)
			return getFlicFileName(unitTypeName, MapUnit.AnimatedAction.DEFAULT);
		else
			throw new Exception(String.Format("Unit type \"{0}\" is missing a default animation.", unitTypeName));
	}

	private Dictionary<string, Util.FlicSheet> flicSheets = new Dictionary<string, Util.FlicSheet>();

	public Util.FlicSheet getFlicSheet(string unitTypeName, MapUnit.AnimatedAction action)
	{
		Util.FlicSheet tr;
		var key = String.Format("{0}.{1}", unitTypeName, action.ToString());
		if (! flicSheets.TryGetValue(key, out tr)) {
			string flicFileName = getFlicFileName(unitTypeName, action);
			(tr, _) = Util.loadFlicSheet(String.Format("Art/Units/{0}/{1}", unitTypeName, flicFileName));
			flicSheets.Add(key, tr);
		}
		return tr;
	}

	public double getDuration(string unitTypeName, MapUnit.AnimatedAction action)
	{
		Util.FlicSheet flicSheet = getFlicSheet(unitTypeName, action);
		double frameCount = flicSheet.indices.GetWidth() / flicSheet.spriteWidth;
		return frameCount / 20.0; // Civ 3 anims often run at 20 FPS   TODO: Do they all? How could we tell? Is it exactly 20 FPS?
	}
}
