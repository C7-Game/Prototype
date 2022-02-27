using Godot;
using System;
using C7Engine;

public class MainMenuMusicPlayer : AudioStreamPlayer
{
	private bool musicEnabled = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Figured out how to load the mp3 from this post: https://godotengine.org/qa/30210/how-do-load-resource-works

		try {
			string mp3Path = Util.Civ3MediaPath("Sounds/Menu/Menu1.mp3");
			File mp3File = new File();
			mp3File.Open(mp3Path, Godot.File.ModeFlags.Read);

			AudioStreamMP3 mp3 = new AudioStreamMP3();
			long fileSize = (long)mp3File.GetLen();	//might blow up if it's > 2 GB, oh well
			mp3.Data = mp3File.GetBuffer(fileSize);
			mp3.Loop = true;
			this.Stream = mp3;

			string volume = C7Settings.GetSettingValue("audio", "musicVolume");
			float targetVolumeOffset = GetVolumeOffset(volume);
			//Intentionally comparing to float.MinValue.  It will match exactly if the INI volume settings is zero
			if (targetVolumeOffset == float.MinValue) {
				musicEnabled = false;
			}

			if (musicEnabled) {
				GD.Print("Setting volume to " + volume + "%, which is " + targetVolumeOffset + " decibel offset");
				int busIndex = AudioServer.GetBusIndex(this.Bus);
				AudioServer.SetBusVolumeDb(busIndex, targetVolumeOffset);
				this.Play();
			}
		}
		catch(ApplicationException ex) {
			GD.PrintErr("Could not load mp3 for main menu music");
		}
	}

	/**
	 * Godot uses a decibel offset volume system, described at https://docs.godotengine.org/en/stable/tutorials/audio/audio_buses.html
	 * This is what audio professionals would use, but is not intuitive to end users.
	 * In this system, a 6db difference halves or doubles the volume.
	 * Our users are probably more used to a 0% to 100% system.
	 * So this method converts between them.
	 */
	private float GetVolumeOffset(string volume)
	{
		if (volume == null) {
			//First run.  Save the setting.
			C7Settings.SetValue("audio", "musicVolume", "100");
			C7Settings.SaveSettings();
			return 0;
		}
		int userVolumeSetting = int.Parse(volume);
		if (userVolumeSetting == 100) {
			return 0;
		}
		else if (userVolumeSetting == 0) {
			return float.MinValue;
		}
		else {
			//Conversion math based on https://stackoverflow.com/a/37810295/3534605
			return 20.0f * (float)(Math.Log10(userVolumeSetting / 100.0f));
		}
	}
}
