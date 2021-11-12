using Godot;
using System;

public class MainMenuMusicPlayer : AudioStreamPlayer
{
	const bool musicEnabled = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Figured out how to load the mp3 from this post: https://godotengine.org/qa/30210/how-do-load-resource-works

		string mp3Path = Util.Civ3MediaPath("Sounds/Menu/Menu1.mp3");
		File mp3File = new File();
		mp3File.Open(mp3Path, Godot.File.ModeFlags.Read);

		AudioStreamMP3 mp3 = new AudioStreamMP3();
		long fileSize = (long)mp3File.GetLen();	//might blow up if it's > 2 GB, oh well
		mp3.Data = mp3File.GetBuffer(fileSize);
		mp3.Loop = true;

		this.Stream = mp3;

		if (musicEnabled) {
			this.Play();
		}
	}
}
