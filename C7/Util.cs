using System;
using System.Collections.Generic;
using Godot;
using ConvertCiv3Media;

public class Util
{
	public class Civ3FileDialog : FileDialog
	// Use this instead of a scene-based FileDialog to avoid it saving the local dev's last browsed folder in the repo
	// While instantiated it will return to the last-accessed folder when reopened
	{
		public string RelPath= "";
		public override void _Ready()
		{
			Mode = ModeEnum.OpenFile;
			Access = AccessEnum.Filesystem;
			CurrentDir = Util.GetCiv3Path() + "/" + RelPath;
			Resizable = true;
			MarginRight = 550;
			MarginBottom = 750;
			base._Ready();
		}
		
	}
	static public string GetCiv3Path()
	{
		// Use CIV3_HOME env var if present
		string path = System.Environment.GetEnvironmentVariable("CIV3_HOME");
		if (path != null) return path;

		// Look up in Windows registry if present
		path = Civ3PathFromRegistry("");
		if (path != "") return path;

		// TODO: Maybe check an array of hard-coded paths during dev time?
		return "/civ3/path/not/found";
	}

	static public string Civ3PathFromRegistry(string defaultPath = "D:/Civilization III")
	{
		// Assuming 64-bit platform, get vanilla Civ3 install folder from registry
		return (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Infogrames Interactive\Civilization III", "install_path", defaultPath);
	}
	static public string Civ3MediaPath(string relPath, string relModPath = "")
	// Pass this function a relative path (e.g. Art/Terrain/xpgc.pcx) and it will grab the correct version
	// Assumes Conquests/Complete
	{
		string Civ3Root = GetCiv3Path();
		string [] TryPaths = new string [] {
			relModPath,
			// Needed for some reason as Steam version at least puts some mod art in Extras instead of Scenarios
			//  Also, the case mismatch is intentional. C3C makes a capital C path, but it's lower-case on the filesystem
			// NOTE: May need another replace for case-sensitive filesystmes (Mac/Linux)
			relModPath.Replace(@"\Civ3PTW\Scenarios\", @"\civ3PTW\Extras\"),
			"Conquests",
			"civ3PTW",
			""
		};
		for(int i = 0; i < TryPaths.Length; i++)
		{
			// If relModPath not set, skip that check
			if(i == 0 && relModPath == "") { continue; }
			string pathCandidate = Civ3Root + "/" + TryPaths[i] + "/" + relPath;
			if(System.IO.File.Exists(pathCandidate)) { return pathCandidate; }
		}
		throw new ApplicationException("Media path not found: " + relPath);
	}

	//Send this function a path (e.g. Art/title.pcx) and it will load it up and convert it to a texture for you.
	static public ImageTexture LoadTextureFromPCX(string relPath)
	{
		if (textureCache.ContainsKey(relPath)) {
			return textureCache[relPath];
		}
		Pcx NewPCX = LoadPCX(relPath);
		ImageTexture texture = PCXToGodot.getImageTextureFromPCX(NewPCX);
		textureCache[relPath] = texture;
		return texture;
	}
	
	private static Dictionary<string, ImageTexture> textureCache = new Dictionary<string, ImageTexture>();
	//Send this function a path (e.g. Art/exitBox-backgroundStates.pcx), and the coordinates of the extracted image you need from that PCX
	//file, and it'll load it up and return you what you need.
	static public ImageTexture LoadTextureFromPCX(string relPath, int leftStart, int topStart, int width, int height)
	{
		string key = relPath + "-" + leftStart + "-" + topStart + "-" + width + "-" + height;
		if (textureCache.ContainsKey(key)) {
			return textureCache[key];
		}
		Pcx NewPCX = LoadPCX(relPath);
		ImageTexture texture = PCXToGodot.getImageTextureFromPCX(NewPCX, leftStart, topStart, width, height);
		textureCache[key] = texture;
		return texture;
	}

	private static Dictionary<string, Pcx> PcxCache = new Dictionary<string, Pcx>();

	/**
	 * Utility method for loading PCX files that will cache them, so we don't have to load them from disk so often.
	 **/
	static public Pcx LoadPCX(string relPath)
	{
		if (PcxCache.ContainsKey(relPath)) {
			return PcxCache[relPath];
		}
		Pcx thePcx = new Pcx(Util.Civ3MediaPath(relPath));
		PcxCache[relPath] = thePcx;
		return thePcx;
	}

	static public AudioStreamSample LoadWAVFromDisk(string path)
	{
		File file = new File();
		file.Open(path, Godot.File.ModeFlags.Read);

		string riffString = System.Text.Encoding.UTF8.GetString(file.GetBuffer(4));
		if (riffString != "RIFF")
		{
			throw new Exception("Unsupported file");
		}
		uint fileSize = file.Get32();	//minus 8 bytes

		string waveString = System.Text.Encoding.UTF8.GetString(file.GetBuffer(4));
		if (waveString != "WAVE")
		{
			throw new Exception("Unsupported file");
		}

		bool formatFound = false;
		bool dataFound = false;
		
		AudioStreamSample wav = new AudioStreamSample();

		while (!file.EofReached())
		{
			string chunk = System.Text.Encoding.UTF8.GetString(file.GetBuffer(4));
			uint chunkSize = file.Get32();
			ulong position = file.GetPosition();

			if (file.EofReached()) {
				//May occur with e.g. an empty junk chunk
				break;
			}

			if (chunk == "fmt ")	//format chunk
			{
				//There is some disagreement between the C++ and GDScript sources
				//as to which compression codes Godot supports.  The C++ has a comment
				//saying, "Consider revision for engine version 3.0", and noting other
				//formats are not supported in its importer.  The GDScript seems
				//to match up with the current FormatEnum.  I'm going to go out on
				//a limb and say the GDScript is probably more current relative
				//to what AudioStreamSample supports.  But that could be wrong.
				ushort compressionCode = file.Get16();
				if (compressionCode == 1) {
					wav.Format = Godot.AudioStreamSample.FormatEnum.Format16Bits;
				}
				else if (compressionCode == 0) {
					wav.Format = Godot.AudioStreamSample.FormatEnum.Format8Bits;
				}
				else if (compressionCode == 2) {
					wav.Format = Godot.AudioStreamSample.FormatEnum.ImaAdpcm;
				}

				ushort channels = file.Get16();
				if (channels == 2) {
					wav.Stereo = true;
				}
				else if (channels < 1 || channels > 5) {
					throw new Exception("Only mono and stream WAV files supported");
				}

				uint sampleRate = file.Get32();
				wav.MixRate = (int)sampleRate;

				uint averageBPS = file.Get32();	//unused
				ushort blockAlign = file.Get16();	//unused
				ushort formatBits = file.Get16();

				if (formatBits % 8 != 0 || formatBits == 0) {
					throw new Exception("Format bits must be a multiple of 8");
				}
				formatFound = true;
			}
			else if (chunk == "data")
			{
				byte[] allTheData = file.GetBuffer(chunkSize);
				wav.Data = allTheData;
				dataFound = true;
			}

			file.Seek((long)(position + chunkSize));
		}

		if (!formatFound || !dataFound) {
			throw new Exception("Failed to find both the format and data chunks");
		}
		
		return wav;
	}
}
