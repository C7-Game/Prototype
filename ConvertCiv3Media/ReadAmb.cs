using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace ConvertCiv3Media;

public struct Sfx {
	public float delayStart; // seconds before start playing
							 // public float duration; // do we even need this?
	public string wavName;
	public bool speedRandom;
	public int minRandomSpeed; // per cent increase/decrease
	public int maxRandomSpeed; // per cent increase/decrease
	public bool volumeRandom;
	public int minRandomVolume; // per cent of original 100%
	public int maxRandomVolume; // per cent of original 100%
}

public class Amb {
	private static ILogger log = Log.ForContext<Amb>();
	private AmbData ambData;
	private string path;

	public List<Sfx> soundEffects { get; set; } = new List<Sfx>();

	public Amb(string path) {
		this.path = path;
		this.ambData = new AmbData(path);
		Process();
	}

	private void Process() {
		var infoTrack = ambData.midiData.soundTracks.FirstOrDefault(t => t.IsInfoTrack());
		if (infoTrack == null)
			throw new Exception("No info track found");

		var secondsPerQuarterNote = infoTrack.setTempoEvent.microsecondsPerQuarterNote / 1_000_000.0f;
		var secondsPerTick = 0.0f;

		if (ambData.midiData.ticksPerQuarterNote > 0) {
			secondsPerTick = secondsPerQuarterNote / ambData.midiData.ticksPerQuarterNote;
		}
		// else ?

		// skip one as it's the info track
		for (int i = 1; i < ambData.midiData.soundTracks.Length; ++i) {
			var st = ambData.midiData.soundTracks[i];
			var delay = st.NoteOnEvent.timeDelta * secondsPerTick;
			var terminates = st.NoteOffEvent.timeDelta * secondsPerTick;

			var prgmChunk = ambData.prgmChunks.Skip(i - 1).First();
			var kmapChunk = ambData.kmapChunks.Skip(i - 1).First();

			var wavFileName = kmapChunk.items[0].wavFileName; // should we be accessing [0] in items?

			if (string.IsNullOrEmpty(wavFileName)) {
				log.Warning($"Missing wavName data in: {this.path}, in index {i - 1}");

				// Attempt to correct some known broken .amb
				// TODO: move to lua maybe?
				if (this.path.EndsWith("ArcherRun.amb")) {
					if (i - 1 == 2) {
						wavFileName = "ArchRunBreath1.wav";
					}
					if (i - 1 == 3) {
						wavFileName = "ArchRunBreath2.wav";
					}
				} else {
					continue;
				}
			}

			var entry = new Sfx() {
				delayStart = delay,
                // duration = terminates - delay,
                wavName = wavFileName,
				speedRandom = prgmChunk.randomizePlaybackSpeed,
				minRandomSpeed = NormalizeSpeed(prgmChunk.minRandomSpeed),
				maxRandomSpeed = NormalizeSpeed(prgmChunk.maxRandomSpeed),
				volumeRandom = prgmChunk.randomizeVolume,
				minRandomVolume = NormalizeVolume(prgmChunk.minRandomVolume),
				maxRandomVolume = NormalizeVolume(prgmChunk.maxRandomVolume),
			};

			soundEffects.Add(entry);
		}

		soundEffects = soundEffects.OrderBy(e => e.delayStart).ToList();
	}

	// the amb data being midi, have an upper bound of 127,
	// so we transform it to an even 100 so that we can work with it more easily
	private int NormalizeVolume(int volume) {
		float result = volume * (100f / 127);
		return (int)result;
	}
	// 100 speed corresponds to roughly 6% increase/decrease in speed, that's where the 6 is coming from.
	// So an item with speed 200, will play ~12% faster, and with speed -200 will play ~12% slower
	private int NormalizeSpeed(int speed) {
		float result = 6f * (speed / 100f);
		return (int)result;
	}
}

/*
    Some useful links on parsing .amb & .mid(i) files
    
    https://www.recordingblogs.com/wiki/midi-meta-messages
    https://www.mixagesoftware.com/en/midikit/help/HTML/midi_events.html
    https://www.mixagesoftware.com/en/midikit/help/HTML/meta_events.html
    https://www.youtube.com/watch?v=P27ml4M3V7A
    https://forums.civfanatics.com/threads/amb-sound-editor.698491/
    https://github.com/maxpetul/Civ3AMBAnalysis/blob/master/AMBFormat.org
    https://github.com/maxpetul/C3X/blob/master/AMB%20Editor/amb_file.c
    https://www.skytopia.com/project/articles/midi.html    
 */
public class AmbData {
	private static ILogger log = Log.ForContext<AmbData>();

	public List<PrgmChunk> prgmChunks = new List<PrgmChunk>();
	public List<KmapChunk> kmapChunks = new List<KmapChunk>();
	public GlblChunk glblChunk;
	public MidiData midiData;

	public AmbData(string path) {
		this.Load(path);
	}

	private struct VarLenItem(int len, int val) {
		public int length = len;
		public int value = val;
	}

	private const int HEADER_SIZE = 8; // 4 for header tag + 4 for the size field itself

	private int timeDeltaOffset = 0;

	private System.Text.ASCIIEncoding ascii = new System.Text.ASCIIEncoding();

	private void Load(string path) {
		if (!path.EndsWith(".amb", StringComparison.CurrentCultureIgnoreCase)) {
			throw new ApplicationException($"Invalid file type for file: `{path}`. Only .amb files are supported.");
		}
		log.Information($"Parsing `{path}`");

		byte[] ambBytes = File.ReadAllBytes(path);

		int offset = 0;

		int soundTrackNum = 0;

		while (offset < ambBytes.Length) {
			int header = BitConverter.ToInt32(ambBytes, offset);
			timeDeltaOffset = 0; // this accumulates for every event in a track so we need to reset it in every new track

			switch (header) {
				case 0x6d677270: // prgm
					var prgm = new PrgmChunk() {
						size = BitConverter.ToInt32(ambBytes, offset + 4), // does not count itself or the header tag
                        index = BitConverter.ToInt32(ambBytes, offset + 8),
						randomizePlaybackSpeed = GetFlag(ambBytes[offset + 12], 0),
						randomizeVolume = GetFlag(ambBytes[offset + 12], 1),
						maxRandomSpeed = BitConverter.ToInt32(ambBytes, offset + 16),
						minRandomSpeed = BitConverter.ToInt32(ambBytes, offset + 20),
						maxRandomVolume = BitConverter.ToInt32(ambBytes, offset + 24),
						minRandomVolume = BitConverter.ToInt32(ambBytes, offset + 28),
					};
					// skip 4 bytes for 0xFA that terminates the chunk (early)
					var eff = GetNullTerminatedString(ambBytes, offset + 36);
					prgm.effectName = System.Text.Encoding.UTF8.GetString(eff.data);
					var var = GetNullTerminatedString(ambBytes, offset + 36 + eff.size + 1); // +1 to account for the terminating byte 0x00
					prgm.varName = System.Text.Encoding.UTF8.GetString(var.data);
					this.prgmChunks.Add(prgm);
					offset += prgm.size + HEADER_SIZE;
					break;
				case 0x70616d6b: // kmap
					var varName = GetNullTerminatedString(ambBytes, offset + 20);
					var kmap = new KmapChunk() {
                        // the size is not always accurate; e.x. GalleyAttack.amb
                        size = BitConverter.ToInt32(ambBytes, offset + 4), // does not count itself or the header tag
                        unknownFlag1 = GetFlag(ambBytes[offset + 8], 0),
						unknownFlag2 = GetFlag(ambBytes[offset + 8], 1),
						unknownInt1 = BitConverter.ToInt32(ambBytes, offset + 12),
						unknownInt2 = BitConverter.ToInt32(ambBytes, offset + 16),
						varName = System.Text.Encoding.UTF8.GetString(varName.data),
						itemCount = BitConverter.ToInt32(ambBytes, offset + 20 + varName.size + 1),
						dataSize = BitConverter.ToInt32(ambBytes, offset + 24 + varName.size + 1),
					};
					var chunkSize = 24 + varName.size + 1;

					kmap.items = new KmapItem[kmap.itemCount];
					for (int i = 0; i < kmap.items.Length; i++) {
						var wavFile = GetNullTerminatedString(ambBytes, offset + 40 + varName.size + 1);
						var kmapItem = new KmapItem() {
							size = 12 + wavFile.size + 1,
							unknown1 = BitConverter.ToInt32(ambBytes, offset + 28 + varName.size + 1),
							unknown2 = BitConverter.ToInt32(ambBytes, offset + 32 + varName.size + 1),
							unknown3 = BitConverter.ToInt32(ambBytes, offset + 36 + varName.size + 1),
							wavFileName = System.Text.Encoding.UTF8.GetString(wavFile.data)
						};
						kmap.items[i] = kmapItem;
					}
					this.kmapChunks.Add(kmap);
					offset += chunkSize + HEADER_SIZE;
					foreach (var kmapItem in kmap.items) {
						offset += kmapItem.size;
					}

					break;
				case 0x6c626c67: // glbl
					var glbl = new GlblChunk() {
						size = BitConverter.ToInt32(ambBytes, offset + 4),
						dataSize = BitConverter.ToInt32(ambBytes, offset + 8),
						unknownInt1 =  BitConverter.ToInt32(ambBytes, offset + 12),
						unknownInt2 =  BitConverter.ToInt32(ambBytes, offset + 16),
						terminated = ambBytes.Skip(offset + 20).Take(4).ToArray(),
					};
					this.glblChunk = glbl;
					offset += glbl.size + HEADER_SIZE;
					break;
				// start of Midi section
				case 0x6468544d: // MThd
					var midi = new MidiData() {
						headerSize = GetInt32FromBigEndian(ambBytes.Skip(offset + 4).Take(4).ToArray()),
						midiFormat = GetInt16FromBigEndian(ambBytes.Skip(offset + 8).Take(2).ToArray()),
						trackCount = GetInt16FromBigEndian(ambBytes.Skip(offset + 10).Take(2).ToArray()),
						ticksPerQuarterNote = GetInt16FromBigEndian(ambBytes.Skip(offset + 12).Take(2).ToArray()),
					};

					midi.soundTracks = new SoundTrack[midi.trackCount]; // initialize the sound track array since we know how many tracks we have
					this.midiData = midi;
					offset += 14;
					break;
				case 0x6b72544d: // MTrk
					var trackSize = GetInt32FromBigEndian(ambBytes.Skip(offset + 4).Take(4).ToArray());

					var soundTrack = new SoundTrack() { };

					List<ControlChangeEvent> controlChangeEvents = new List<ControlChangeEvent>();

					offset += HEADER_SIZE;

					var trackOffset = offset + trackSize;

					while (offset < trackOffset) {
						bool isMetaEvent = true;

						var varLenItem = ReadVariableLengthItem(ambBytes, offset);

						var eventType = ambBytes.Skip(offset + varLenItem.length).First();
						var eventId = ambBytes.Skip(offset + varLenItem.length + 1).First();

						var highNibble = -1;
						var lowNibble = -1;

						if (eventType != 0xff) {
							isMetaEvent = false;
							var nibbles = GetNibbles(eventType);
							highNibble = nibbles.highNibble;
							lowNibble = nibbles.lowNibble;
						}

						// Meta events
						if (isMetaEvent && eventId == 0x03) {
							var midiEvent = ParseTrackNameEvent(varLenItem, ambBytes, offset);
							soundTrack.TrackNameEvent = midiEvent;
							offset += midiEvent.size + 4; // + 4 is the event header size; e.x. 0x00ff0305
						} else if (isMetaEvent && eventId == 0x51) {
							var midiEvent = ParseSetTempoEvent(varLenItem, ambBytes, offset);
							soundTrack.setTempoEvent = midiEvent;
							offset += midiEvent.size + 4;
						} else if (isMetaEvent && eventId == 0x54) {
							var midiEvent = ParseSMPTEOffsetEvent(varLenItem, ambBytes, offset);
							soundTrack.smpteOffsetEvent = midiEvent;
							offset += midiEvent.size + 4;
						} else if (isMetaEvent && eventId == 0x58) {
							var midiEvent = ParseTimeSignatureEvent(varLenItem, ambBytes, offset);
							soundTrack.timeSignatureEvent = midiEvent;
							offset += midiEvent.size + 4;
						}

						  // Midi events
						  else if (!isMetaEvent && highNibble == 0x8) {
							var midiEvent = ParseNoteOffEvent(varLenItem, ambBytes, offset, lowNibble);
							soundTrack.NoteOffEvent = midiEvent;
							offset += midiEvent.size;
						} else if (!isMetaEvent && highNibble == 0x9) {
							var midiEvent = ParseNoteOnEvent(varLenItem, ambBytes, offset, lowNibble);
							soundTrack.NoteOnEvent = midiEvent;
							offset += midiEvent.size;
						} else if (!isMetaEvent && highNibble == 0xb) {
							var midiEvent = ParseControlChangeEvent(varLenItem, ambBytes, offset, lowNibble);
							controlChangeEvents.Add(midiEvent);
							offset += midiEvent.size;
						} else if (!isMetaEvent && highNibble == 0xc) {
							var midiEvent = ParseProgramChangeEvent(varLenItem, ambBytes, offset, lowNibble);
							soundTrack.programChangeEvent = midiEvent;
							offset += midiEvent.size;
						}

						  // terminate track
						  else if (isMetaEvent && eventId == 0x2f) {
							offset += 0 + 4;
						} else {
							throw new Exception($"Unknown event id: 0x{eventId:x} ({eventId})");
						}
					}

					soundTrack.controlChangeEvents = controlChangeEvents;

					this.midiData.soundTracks[soundTrackNum] = soundTrack;

					soundTrackNum++;

					break;
				default:
					throw new Exception($"Unknown header: 0x{header:x} ({header}) at offset {offset}");
			}
		}
	}

	// Midi meta events
	private TrackNameEvent ParseTrackNameEvent(VarLenItem varLenItem, byte[] bytes, int offset) {
		int eventOffset = offset + varLenItem.length;
		int eventSize = bytes.Skip(eventOffset + 2).First();
		var midiEvent = new TrackNameEvent() {
			size = eventSize,
			timeDelta = timeDeltaOffset + varLenItem.value,
			trackName = ascii.GetString(bytes, eventOffset + 3, eventSize),
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	private SMPTEOffsetEvent ParseSMPTEOffsetEvent(VarLenItem varLenItem, byte[] bytes, int offset) {
		int eventOffset = offset + varLenItem.length;
		int eventSize = bytes.Skip(eventOffset + 2).First();
		var midiEvent = new SMPTEOffsetEvent() {
			size = eventSize,
			timeDelta = timeDeltaOffset + varLenItem.value,
			framesPerSecond = GetFramesPerSecond(bytes.Skip(eventOffset + 3).First()),
			hours = ExtractBits(bytes.Skip(eventOffset + 3).ToArray().First(), 0, 5),
			minutes = bytes.Skip(eventOffset + 4).First(),
			seconds = bytes.Skip(eventOffset + 5).First(),
			frames = bytes.Skip(eventOffset + 6).First(),
			subFrames = bytes.Skip(eventOffset + 7).First(),
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	private TimeSignatureEvent ParseTimeSignatureEvent(VarLenItem varLenItem, byte[] bytes, int offset) {
		int eventOffset = offset + varLenItem.length;
		int eventSize = bytes.Skip(eventOffset + 2).First();
		var midiEvent = new TimeSignatureEvent {
			size = eventSize,
			timeDelta = timeDeltaOffset + varLenItem.value,
			numerator = bytes.Skip(eventOffset + 3).First(),
			pow = bytes.Skip(eventOffset + 4).First(),
			metronomePulse = bytes.Skip(eventOffset + 5).First(),
			num32NotesPerBeat = bytes.Skip(eventOffset + 6).First(),
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	private SetTempoEvent ParseSetTempoEvent(VarLenItem varLenItem, byte[] bytes, int offset) {
		int eventOffset = offset + varLenItem.length;
		int eventSize = bytes.Skip(eventOffset + 2).First();
		// create an int from 3 bytes, in big endian mode
		int value = GetInt24FromBigEndian(bytes.Skip(eventOffset + 3).Take(3).ToArray());
		var midiEvent = new SetTempoEvent {
			size = eventSize,
			timeDelta = timeDeltaOffset + varLenItem.value,
			microsecondsPerQuarterNote = value
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	// Midi events
	private NoteOffEvent ParseNoteOffEvent(VarLenItem varLenItem, byte[] bytes, int offset, int lowNibble) {
		int eventOffset = offset + varLenItem.length;
		var midiEvent = new NoteOffEvent {
			size = varLenItem.length + 3,
			timeDelta = timeDeltaOffset + varLenItem.value,
			channelNumber = lowNibble,
			key = bytes.Skip(eventOffset + 1).First(),
			velocity = bytes.Skip(eventOffset + 2).First(),
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	private NoteOnEvent ParseNoteOnEvent(VarLenItem varLenItem, byte[] bytes, int offset, int lowNibble) {
		int eventOffset = offset + varLenItem.length;
		var midiEvent = new NoteOnEvent {
			size = varLenItem.length + 3,
			timeDelta = timeDeltaOffset + varLenItem.value,
			channelNumber = lowNibble,
			key = bytes.Skip(eventOffset + 1).First(),
			velocity = bytes.Skip(eventOffset + 2).First(),
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	private ControlChangeEvent ParseControlChangeEvent(VarLenItem varLenItem, byte[] bytes, int offset, int lowNibble) {
		int eventOffset = offset + varLenItem.length;
		var midiEvent = new ControlChangeEvent {
			size = varLenItem.length + 3,
			timeDelta = timeDeltaOffset + varLenItem.value,
			channelNumber = lowNibble,
			controllerNumber = bytes.Skip(eventOffset + 1).First(),
			value = bytes.Skip(eventOffset + 2).First(),
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	private ProgramChangeEvent ParseProgramChangeEvent(VarLenItem varLenItem, byte[] bytes, int offset, int lowNibble) {
		int eventOffset = offset + varLenItem.length;
		var midiEvent = new ProgramChangeEvent {
			size = varLenItem.length + 2,
			timeDelta = timeDeltaOffset + varLenItem.value,
			channelNumber = lowNibble,
			programNumber = bytes.Skip(eventOffset + 1).First(),
		};
		timeDeltaOffset += midiEvent.timeDelta;
		return midiEvent;
	}

	// a nibble is half a byte
	// this returns the two nibbles, read from left to right
	// so, oxB3 will return
	// high : B(11 in decimal) and low : 3
	private (int highNibble, int lowNibble) GetNibbles(byte bite) {
		var high = (byte)(bite >> 4);
		var low = (byte)(bite & 0x0F);
		return (high, low);
	}

	private float GetFramesPerSecond(byte bite) {
		bool a = GetFlag(bite, 5);
		bool b = GetFlag(bite, 6);

		if (a && b) return 30f;
		if (a && !b) return 29.97f;
		if (!a && b) return 25f;

		// !a && !b
		return 24f;
	}

	private int ExtractBits(byte value, int startBit, int bitCount) {
		int mask = (1 << bitCount) - 1;
		return (value >> startBit) & mask;
	}

	private (int size, byte[] data) GetNullTerminatedString(byte[] bytes, int offset) {
		List<byte> data = new List<byte>();
		while (bytes[offset] != 0x00) {
			data.Add(bytes[offset]);
			offset++;
		}

		var d = data.ToArray();
		return (d.Length, d);
	}

	private bool GetFlag(byte b, int index) {
		return (b & (1 << index)) != 0;
	}

	private int GetInt32FromBigEndian(byte[] bytes) {
		int value = bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
		return value;
	}
	private int GetInt24FromBigEndian(byte[] bytes) {
		int value = bytes[0] << 16 | bytes[1] << 8 | bytes[2];
		return value;
	}
	private short GetInt16FromBigEndian(byte[] bytes) {
		int value =  bytes[0] << 8 | bytes[1];
		return (short)value;
	}

	private VarLenItem ReadVariableLengthItem(byte[] bytes, int offset) {
		int val = 0;
		int len = 0;
		while (true) {
			val |= (bytes[offset + len] & 0x7f);
			if ((bytes[offset + len] & 0x80) != 0) {
				val <<= 7;
				len += 1;
			} else {
				break;
			}
		}

		return new VarLenItem(len + 1, val);
	}
}

// In Prgm, Kmap, and Glbl chunks, integers are little endian and strings are null terminated.

// tag prgm
public class PrgmChunk {
	public int size;
	public int index;

	public bool randomizePlaybackSpeed;
	public bool randomizeVolume;

	public int maxRandomSpeed;
	public int minRandomSpeed;
	public int maxRandomVolume;
	public int minRandomVolume;

	public string effectName;
	public string varName; // matches KmapChunk varName
}

public class KmapItem {
	public int size;
	public int unknown1;
	public int unknown2;
	public int unknown3;

	public string wavFileName;
}

// tag kmap
public class KmapChunk {
	public int size;

	public bool unknownFlag1;
	public bool unknownFlag2;

	public int unknownInt1;
	public int unknownInt2;

	public string varName; // matches PrgmChunk varName

	public int itemCount;
	public int dataSize;

	public KmapItem[] items;
}

// tag glbl
public class GlblChunk {
	public int size;
	public int dataSize;
	// I don't know what this is, but instead of having an array of bytes
	// I will break it up to 2 ints + byte[4] array
	public int unknownInt1;
	public int unknownInt2;
	public byte[] terminated; // 0xCDCDCDCD (always?)
}

// Midi file integers are big endian and strings are not null-terminated

// tag MThd
public class MidiData {
	public int headerSize; // 6 for standard midi files
	public short midiFormat; // 0, 1 or 2, in our case it's always 1
	public short trackCount; // Always >= 2 and <= 13 in our case
	public short ticksPerQuarterNote; // “Division” in the Midi spec. All AMBs in Civ 3 use “metric time”, i.e., this field specifies the length of a quarter note in delta time ticks
									  // The first track contains no sound data, just info about the tempo
	public SoundTrack[] soundTracks;
}

public class SoundTrack {
	public TrackNameEvent TrackNameEvent;
	public SMPTEOffsetEvent smpteOffsetEvent; // only on 1st track (info track)
	public TimeSignatureEvent timeSignatureEvent; // only on 1st track (info track)
	public SetTempoEvent setTempoEvent; // only on 1st track (info track)
	public List<ControlChangeEvent> controlChangeEvents;
	public ProgramChangeEvent programChangeEvent;
	public NoteOnEvent NoteOnEvent;
	public NoteOffEvent NoteOffEvent;

	public bool IsInfoTrack() {
		return this.smpteOffsetEvent != null;
	}
}

/* Midi Meta events 0xff*/

// 0x03
public sealed class TrackNameEvent : MidiEvent {
	public string trackName;
}

// 0x54
public sealed class SMPTEOffsetEvent : MidiEvent {
	public float framesPerSecond;
	public int hours;
	public int minutes;
	public int seconds;
	public int frames;
	public int subFrames;
}

// 0x58
public sealed class TimeSignatureEvent : MidiEvent {
	public int numerator; // is the numerator of the time signature and has values between 0x00 and 0xFF 
	public int pow; // is the power to which the number 2 must be raised to obtain the time signature denominator
	public int metronomePulse; // defines a metronome pulse in terms of the number of MIDI clock ticks per click
	public int num32NotesPerBeat; // defines the number of 32nd notes per beat
}

// 0x51
public sealed class SetTempoEvent : MidiEvent {
	public int microsecondsPerQuarterNote;
}

/* Midi Events */

// 0x8n
public sealed class NoteOffEvent : MidiEvent {
	public int channelNumber;
	public int key;
	public int velocity;
}

// 0x9n
public sealed class NoteOnEvent : MidiEvent {
	public int channelNumber;
	public int key;
	public int velocity;
}

// 0xBn
public sealed class ControlChangeEvent : MidiEvent {
	public int channelNumber;
	public int controllerNumber;
	public int value;
}

// 0xCn
public sealed class ProgramChangeEvent : MidiEvent {
	public int channelNumber;
	public int programNumber;
}

public abstract class MidiEvent {
	public int size;
	public int timeDelta;
}
