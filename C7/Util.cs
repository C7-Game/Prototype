using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using C7Engine;
using Godot;
using ConvertCiv3Media;
using C7GameData;
using QueryCiv3;

public partial class Util {
	static public string Civ3Root = Civ3Location.GetCiv3Path();
	public partial class Civ3FileDialog : FileDialog
	// Use this instead of a scene-based FileDialog to avoid it saving the local dev's last browsed folder in the repo
	// While instantiated it will return to the last-accessed folder when reopened
	{
		public string RelPath= "";
		public Civ3FileDialog(FileDialog.FileModeEnum mode = FileDialog.FileModeEnum.OpenFile) {
			FileMode = mode;
		}

		public override void _Ready() {
			Access = AccessEnum.Filesystem;
			CurrentDir = Civ3Root + "/" + RelPath;
			// Resizable = true;
			// OffsetRight = 550;
			// OffsetBottom = 750;
			base._Ready();
		}
	}

	static private string SteamCommonDir() {
		string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
		if (home == null) { return null; }
		return System.IO.Path.Combine(home, "Library/Application Support/Steam/steamapps/common");
	}

	static private bool FolderIsCiv3(System.IO.DirectoryInfo di) {
		return di.EnumerateFiles().Any(f => f.Name == "civ3id.mb");
	}

	static public string GetCiv3Path() {
		// Use CIV3_HOME env var if present
		string path = System.Environment.GetEnvironmentVariable("CIV3_HOME");
		if (path != null) { return path; }

		// Use settings value if present
		path = C7Settings.GetSettingValue("locations", "civ3InstallDir");
		if (path != null) return path;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			// Look up in Windows registry if present
			path = Civ3PathFromRegistry();
			if (path != null) { return path; }
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			// Check for a civ3 folder in steamapps/common
			System.IO.DirectoryInfo root = new System.IO.DirectoryInfo(Util.SteamCommonDir());
			foreach (System.IO.DirectoryInfo di in root.GetDirectories()) {
				if (Util.FolderIsCiv3(di)) {
					return di.FullName;
				}
			}
		}

		return "/civ3/path/not/found";
	}

	static public string Civ3PathFromRegistry() {
		// Assuming 64-bit platform, get vanilla Civ3 install folder from registry
		// Return null if value not present or if key not found
		object path = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Infogrames Interactive\Civilization III", "install_path", null);
		return path == null ? null : (string)path;
	}

	// Checks if a file exists ignoring case on the latter parts of its path. If the file is found, returns its full path re-capitalized as
	// necessary, otherwise returns null. This function is needed for the game to work on Linux & Mac with the .NET Core runtime. It's not needed
	// on Windows, which has a case insensitive filesystem, or when using the Mono runtime, which emulates case insensitivity out of the
	// box. Arguments:
	//   exactCaseRoot: The first part of the file path, not made case-insensitive. This is intended be the root Civ 3 path from GetCiv3Path().
	//   ignoredCaseExtension: The second part of the file path that will be searched ignoring case.
	public static string FileExistsIgnoringCase(string exactCaseRoot, string ignoredCaseExtension) {
		// First try the basic built-in File.Exists method since it's adequate in most cases.
		string fullPath = System.IO.Path.Combine(exactCaseRoot, ignoredCaseExtension);
		if (System.IO.File.Exists(fullPath))
			return fullPath;

		// If that didn't work, do a case-insensitive search starting at the root path and stepping through each piece of the extension. Skip
		// this step if the root directory doesn't exist or if running on Windows.
		string tr = null;
		if ((!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) &&
			System.IO.Directory.Exists(exactCaseRoot)) {
			tr = exactCaseRoot;
			foreach (string step in ignoredCaseExtension.Replace('\\', '/').Split('/')) {
				string goal = System.IO.Path.Combine(tr, step);
				List<string> matches = System.IO.Directory.EnumerateFileSystemEntries(tr, "*")
					.Where(p => p.Equals(goal, StringComparison.CurrentCultureIgnoreCase))
					.ToList();
				if (matches.Count > 0)
					tr = matches[0];
				else {
					tr = null;
					break;
				}
			}
		}
		return tr;
	}

	/// <summary>
	/// Sets the Civ3 legacy mod path.
	/// This is here so Civ3MediaPath can refer to it, without having to grab it from all the places we might need to call
	/// it, which is in 25 places currently.
	/// </summary>
	private static string modPath;
	public static void setModPath(string modPathParam) {
		modPath = modPathParam;
	}

	/// <summary>
	/// Pass this function a relative path (e.g. Art/Terrain/xpgc.pcx) and it will grab the correct version
	/// Assumes Conquests/Complete
	/// </summary>
	/// <param name="mediaPath">The media path, e.g. Art/Units/units_32.pcx</param>
	/// <param name="modPath">The mod path for a scenario, e.g. RFRE</param>
	/// <returns>The path to the media on the file system, or an exception if it cannot be found</returns>
	/// <exception cref="ApplicationException"></exception>
	public static string Civ3MediaPath(string mediaPath) {
		//First, check if the file exists via a scenario's mod path
		//For now this is only checked relative to Civ3, not relative to C7.
		if (!string.IsNullOrEmpty(modPath)) {
			string[] paths = modPath.Split(";");
			foreach (string path in paths) {
				string[] tryPaths = new string[] {
					path,
					// Needed for some reason as Steam version at least puts some mod art in Extras instead of Scenarios
					//  Also, the case mismatch is intentional. C3C makes a capital C path, but it's lower-case on the filesystem
					"Conquests/Conquests/" + path, "Conquests/Scenarios/" + path, "civ3PTW/Scenarios/" + path
				};
				for (int i = 0; i < tryPaths.Length; i++) {
					string actualCasePath = CheckForCiv3Media(mediaPath, tryPaths[i]);
					if (actualCasePath != null)
						return actualCasePath;
				}
			}
		}

		//Next, before trying the base Civ paths, see if we have it packaged with C7
		string c7Path = FileExistsIgnoringCase("", mediaPath);
		if (c7Path != null) {
			return c7Path;
		}

		//Finally, check the base Civ paths
		string[] basePaths = new string[] {
			"Conquests",
			"civ3PTW",
			""
		};
		for (int i = 0; i < basePaths.Length; i++) {
			string actualCasePath = CheckForCiv3Media(mediaPath, basePaths[i]);
			if (actualCasePath != null)
				return actualCasePath;
		}

		throw new ApplicationException("Media path not found: " + mediaPath);
	}

	private static string CheckForCiv3Media(string relPath, string rootPath) {
		// Combine TryPaths[i] and relPath. Make sure not to leave an erroneous forward slash at the start if TryPaths[i] is empty
		string fullPath = rootPath != "" ? rootPath + "/" + relPath : relPath;

		return FileExistsIgnoringCase(Civ3Root, fullPath);
	}

	//Send this function a path (e.g. Art/title.pcx) and it will load it up and convert it to a texture for you.
	static public ImageTexture LoadTextureFromPCX(string relPath) {
		if (textureCache.ContainsKey(relPath)) {
			return textureCache[relPath];
		}
		Pcx NewPCX = LoadPCX(relPath);
		ImageTexture texture = PCXToGodot.getImageTextureFromPCX(NewPCX);
		textureCache[relPath] = texture;
		return texture;
	}

	static public (ImageTexture, ImageTexture) LoadTextureFromFlicData(byte[] image, byte[,] pallete, int width, int height) {
		var (baseImage, tintImage) = PCXToGodot.ByteArrayWithTintToImage(image, pallete, width, height, shadows: true);
		return (ImageTexture.CreateFromImage(baseImage), ImageTexture.CreateFromImage(tintImage));
	}

	static public Flic LoadFlic(string path) {
		return new ConvertCiv3Media.Flic(Util.Civ3MediaPath(path));
	}

	static private string getProjectDirectoryPath() {
		// see issue https://github.com/godotengine/godot/issues/24222#issuecomment-709092664
		// - use local resource folder in debug mode
		// - use executable folder in release mode
		return OS.IsDebugBuild() ? "res://" : OS.GetExecutablePath().GetBaseDir();
	}

	// Send this function a path (e.g. Title_Screen.jpg) and it will
	// load it up and convert it in both debug and release modes.
	// Note: We probably will need variants of this for other file types, too.
	static public ImageTexture LoadTextureFromC7JPG(string relPath) {
		Image img = Image.LoadFromFile(Util.getProjectDirectoryPath().PathJoin(relPath));
		return ImageTexture.CreateFromImage(img);
	}

	private static Dictionary<string, ImageTexture> textureCache = new Dictionary<string, ImageTexture>();
	//Send this function a path (e.g. Art/exitBox-backgroundStates.pcx), and the coordinates of the extracted image you need from that PCX
	//file, and it'll load it up and return you what you need.
	static public ImageTexture LoadTextureFromPCX(string relPath, int leftStart, int topStart, int width, int height, bool shadows = true) {
		string key = relPath + "-" + leftStart + "-" + topStart + "-" + width + "-" + height;
		if (textureCache.ContainsKey(key)) {
			return textureCache[key];
		}
		Pcx NewPCX = LoadPCX(relPath);
		ImageTexture texture = PCXToGodot.getImageTextureFromPCX(NewPCX, leftStart, topStart, width, height, shadows);
		textureCache[key] = texture;
		return texture;
	}

	static public ImageTexture LoadFogOfWarPCX(string relPath) => PCXToGodot.getImageTextureFromFogOfWarPCX(LoadPCX(relPath));

	private static Dictionary<string, Pcx> PcxCache = new Dictionary<string, Pcx>();

	/**
	 * Utility method for loading PCX files that will cache them, so we don't have to load them from disk so often.
	 **/
	static public Pcx LoadPCX(string relPath) {
		if (PcxCache.ContainsKey(relPath)) {
			return PcxCache[relPath];
		}
		Pcx thePcx = new Pcx(Civ3MediaPath(relPath));
		PcxCache[relPath] = thePcx;
		return thePcx;
	}

	// Creates a texture from raw palette data. The data must be 256 pixels by 3 channels. Returns a 16x16 unfiltered RGB texture.
	public static ImageTexture createPaletteTexture(byte[,] raw) {
		if ((raw.GetLength(0) != 256) || (raw.GetLength(1) != 3))
			throw new Exception("Invalid palette dimensions. Palettes must be 256x3.");

		// Flatten palette data since CreateFromData can't accept two-dimensional arrays
		byte[] flatPalette = new byte[3*256];
		for (int n = 0; n < 256; n++)
			for (int k = 0; k < 3; k++)
				flatPalette[k + 3 * n] = raw[n, k];

		var img = Image.CreateFromData(16, 16, false, Image.Format.Rgb8, flatPalette);
		return ImageTexture.CreateFromImage(img);
	}

	// Creates textures from a PCX file without de-palettizing it. Returns two ImageTextures, the first is 16x16 with RGB8 format containing the
	// color palette and the second is the size of the image itself and contains the indices in R8 format.
	public static (ImageTexture palette, ImageTexture indices) loadPalettizedPCX(string filePath) {
		var pcx = LoadPCX(filePath);

		var imgIndices = Image.CreateFromData(pcx.Width, pcx.Height, false, Image.Format.R8, pcx.ColorIndices);
		ImageTexture texIndices = ImageTexture.CreateFromImage(imgIndices);

		return (createPaletteTexture(pcx.Palette), texIndices);
	}

	// A FlicSheet is a sprite sheet created from a Flic file, with each frame of the animation as its own sprite
	public struct FlicSheet {
		public ImageTexture palette, indices;
		public int spriteWidth, spriteHeight;
	}

	// Loads a Flic and also converts it into a sprite sheet
	public static (FlicSheet, Flic) loadFlicSheet(string filePath) {
		var flic = new Flic(Util.Civ3MediaPath(filePath));

		var texPalette = Util.createPaletteTexture(flic.Palette);

		var countColumns = flic.Images.GetLength(1); // Each column contains one frame
		var countRows = flic.Images.GetLength(0); // Each row contains one animation
		var countImages = countColumns * countRows;

		byte[] allIndices = new byte[countRows * countColumns * flic.Width * flic.Height];
		// row, col loop over the sprites, each one a frame of the animation
		for (int row = 0; row < countRows; row++)
			for (int col = 0; col < countColumns; col++)
				// x, y loop over pixels within each sprite
				for (int y = 0; y < flic.Height; y++)
					for (int x = 0; x < flic.Width; x++) {
						int pixelRow = row * flic.Height + y,
							pixelCol = col * flic.Width + x,
							pixelIndex = pixelRow * countColumns * flic.Width + pixelCol;
						allIndices[pixelIndex] = flic.Images[row, col][y * flic.Width + x];
					}

		var imgIndices = Image.CreateFromData(countColumns * flic.Width, countRows * flic.Height, false, Image.Format.R8, allIndices);
		ImageTexture texIndices = ImageTexture.CreateFromImage(imgIndices);

		return (new FlicSheet { palette = texPalette, indices = texIndices, spriteWidth = flic.Width, spriteHeight = flic.Height }, flic);
	}

	static public AudioStreamWav LoadWAVFromDisk(string path) {
		FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

		string riffString = System.Text.Encoding.UTF8.GetString(file.GetBuffer(4));
		if (riffString != "RIFF") {
			throw new Exception("Unsupported file");
		}
		uint fileSize = file.Get32();   //minus 8 bytes

		string waveString = System.Text.Encoding.UTF8.GetString(file.GetBuffer(4));
		if (waveString != "WAVE") {
			throw new Exception("Unsupported file");
		}

		bool formatFound = false;
		bool dataFound = false;

		AudioStreamWav wav = new AudioStreamWav();

		while (!file.EofReached()) {
			string chunk = System.Text.Encoding.UTF8.GetString(file.GetBuffer(4));
			uint chunkSize = file.Get32();
			ulong position = file.GetPosition();

			if (file.EofReached()) {
				//May occur with e.g. an empty junk chunk
				break;
			}

			if (chunk == "fmt ")    //format chunk
			{
				//There is some disagreement between the C++ and GDScript sources
				//as to which compression codes Godot supports.  The C++ has a comment
				//saying, "Consider revision for engine version 3.0", and noting other
				//formats are not supported in its importer.  The GDScript seems
				//to match up with the current FormatEnum.  I'm going to go out on
				//a limb and say the GDScript is probably more current relative
				//to what AudioStreamWAV supports.  But that could be wrong.
				ushort compressionCode = file.Get16();
				if (compressionCode == 1) {
					wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
				} else if (compressionCode == 0) {
					wav.Format = AudioStreamWav.FormatEnum.Format8Bits;
				} else if (compressionCode == 2) {
					wav.Format = AudioStreamWav.FormatEnum.ImaAdpcm;
				}

				ushort channels = file.Get16();
				if (channels == 2) {
					wav.Stereo = true;
				} else if (channels < 1 || channels > 5) {
					throw new Exception("Only mono and stream WAV files supported");
				}

				uint sampleRate = file.Get32();
				wav.MixRate = (int)sampleRate;

				uint averageBPS = file.Get32(); //unused
				ushort blockAlign = file.Get16();   //unused
				ushort formatBits = file.Get16();

				if (formatBits % 8 != 0 || formatBits == 0) {
					throw new Exception("Format bits must be a multiple of 8");
				}
				formatFound = true;
			} else if (chunk == "data") {
				byte[] allTheData = file.GetBuffer(chunkSize);
				wav.Data = allTheData;
				dataFound = true;
			}

			file.Seek(position + chunkSize);
		}

		if (!formatFound || !dataFound) {
			throw new Exception("Failed to find both the format and data chunks");
		}

		return wav;
	}
}
