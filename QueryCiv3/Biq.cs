using System;
using System.Collections.Generic;
using QueryCiv3.Biq;

namespace QueryCiv3 {
	public class BiqData {
		public Civ3File FileData;
		public bool HasCustomRules => FileData.SectionExists("BLDG");
		public bool HasCustomMap => FileData.SectionExists("WCHR");

		// RULE, WCHR, WMAP, and GAME all seem to only ever have a maximum of 1 header in the biq files, but I'm not certain on that
		// If that's confirmed to be the case, they can be demoted from arrays to singular variables and the code simplified accordingly
		public BLDG[] Bldg;
		public CITY[] City;
		public CLNY[] Clny;
		public CONT[] Cont;
		public CTZN[] Ctzn;
		public CULT[] Cult;
		public DIFF[] Diff;
		public ERAS[] Eras;
		public ESPN[] Espn;
		public EXPR[] Expr;
		public FLAV[] Flav;
		public GAME[] Game;
		public GOOD[] Good;
		public GOVT[] Govt;
		public LEAD[] Lead;
		public PRTO[] Prto;
		public RACE[] Race;
		public RULE[] Rule;
		public SLOC[] Sloc;
		public TECH[] Tech;
		public TERR[] Terr;
		public TFRM[] Tfrm;
		public TILE[] Tile;
		public UNIT[] Unit;
		public WCHR[] Wchr;
		public WMAP[] Wmap;
		public WSIZ[] Wsiz;

		/*
			Note which of the following are rectangular 2d arrays [,] vs which are jagged 2d arrays [][]
			A rectangular array means that the second dimension for all components is the same eg. for RaceEra, every civ shares the same # of Eras
			A jagged array means that the second dimension can vary between components eg. for RaceCityName, each civ can have a different # of Cities
		*/
		public bool[,] TerrGood; // which resources are allowed on which types of terrain
		public GOVT_GOVT[,] GovtGovt; // relationships between governments
		public RACE_City[][] RaceCityName; // the list of city names for each civ
		public RACE_ERAS[,] RaceEra; // the file names for each era for each civ
		public RACE_LeaderName[][] RaceGreatLeaderName; // the great leaders for each civ
		public RACE_LeaderName[][] RaceScientificLeaderName; // the scientific leaders for each civ
		public int[][] CityBuilding; // Building IDs in each city
		public int[][] WmapResource;
		public int[][] PrtoPrto; // Stealth unit targets per unit
		public LEAD_Unit[][] LeadPrto; // starting unit data for each leader
		public int[][] LeadTech; // starting tech data for each leader
		public RULE_CULT[][] RuleCult; // culture level names per rule
		public int[][] RuleSpaceship; // spaceship quantity requirements per rule
		public int[][] GameCiv; // Playable civs for game
		public int[][] GameAlliance; // Civ alliances for game

		private const int SECTION_HEADERS_START = 736;
		// Dynamic sections need to have their static subcomponents read in as discrete chunks, which these length constants help with
		// The sum of the LEN constants for each section equals the total size of that section's struct
		// eg. GOVT_LEN_1 + GOVT_LEN_2 == sizeof(GOVT)
		private const int GOVT_LEN_1 =  400;
		private const int GOVT_LEN_2 =   76;
		private const int TERR_LEN_1 =    8;
		private const int TERR_LEN_2 =  225;
		private const int RACE_LEN_1 =    8;
		private const int RACE_LEN_2 =    4;
		private const int RACE_LEN_3 =  208;
		private const int RACE_LEN_4 =   92;
		private const int CITY_LEN_1 =   38;
		private const int CITY_LEN_2 =   36;
		private const int WMAP_LEN_1 =    8;
		private const int WMAP_LEN_2 =  164;
		private const int PRTO_LEN_1 =  238;
		private const int PRTO_LEN_2 =   21;
		private const int LEAD_LEN_1 =   56;
		private const int LEAD_LEN_2 =    8;
		private const int LEAD_LEN_3 =   33;
		private const int RULE_LEN_1 =  104;
		private const int RULE_LEN_2 =  164;
		private const int RULE_LEN_3 =   32;
		private const int GAME_LEN_1 =   16;
		private const int GAME_LEN_2 = 5304;
		private const int GAME_LEN_3 = 2017;

		public string Title;
		public string Description;

		public static unsafe BiqData LoadFile(string biqFilePath) {
			byte[] biqBytes = Util.ReadFile(biqFilePath);
			return new BiqData(biqBytes);
		}

		public unsafe BiqData(byte[] biqBytes) {
			Load(biqBytes);
		}

		public unsafe void Load(byte[] biqBytes) {
			FileData = new Civ3File(biqBytes);
			Description = FileData.GetString(32, 640);
			Title = FileData.GetString(672, 64);

			fixed (byte* bytePtr = biqBytes) {
				// For now, we're skipping over the VER# and BIQ file header information to get right to the structs
				// The first section is likely to be BLDG in BIQ files, but the current approach supports any ordering of the sections
				int offset = SECTION_HEADERS_START;
				string header;
				int count = 0;
				int dataLength = 0;

				while (offset < biqBytes.Length) { // Don't read past the end
					// We don't know what orders the headers come in or which headers will be set, so get the next header and switch off it:
					header = FileData.GetString(offset, 4);
					count = FileData.ReadInt32(offset + 4);
					offset += 8;

					// Section data structures are stored in the BiqSections/ folder
					// We can divide the BIQ sections into two types: static and dynamic
					// Static have a fixed length always, which means they can be read directly memory-copied into our structs with no special logic
					//   The static sections are: BLDG, CTZN, CULT, DIFF, ERAS, ESPN, EXPR, FLAV(??), GOOD, TECH, TFRM, WSIZ, WCHR, TILE, CONT, SLOC, UNIT, CLNY
					// Dynamic sections have at least one component with varying length, and so require multiple structs and special logic
					//   The dynamic sections are: GOVT, RULE, PRTO, RACE, TERR, WMAP, CITY, GAME, LEAD
					switch (header) {
						case "BLDG":
							dataLength = count * sizeof(BLDG);
							Bldg = new BLDG[count];
							fixed (void* ptr = Bldg) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "CITY":
							dataLength = 0;
							City = new CITY[count];
							CityBuilding = new int[count][];
							int buildingRowLength = 0;

							fixed (void* ptr = City) {
								byte* cityPtr = (byte*)ptr;
								byte* dataPtr = bytePtr + offset;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, cityPtr, CITY_LEN_1, CITY_LEN_1);
									CityBuilding[i] = new int[City[i].NumberOfBuildings];
									buildingRowLength = City[i].NumberOfBuildings * sizeof(int);
									fixed (void* ptr2 = CityBuilding[i]) Buffer.MemoryCopy(dataPtr + CITY_LEN_1, ptr2, buildingRowLength, buildingRowLength);
									Buffer.MemoryCopy(dataPtr + CITY_LEN_1 + buildingRowLength, cityPtr + CITY_LEN_1, CITY_LEN_2, CITY_LEN_2);

									cityPtr += sizeof(CITY);
									dataPtr += City[i].Length + 4;
									dataLength += City[i].Length + 4;
								}
							}
							break;
						case "CLNY":
							dataLength = count * sizeof(CLNY);
							Clny = new CLNY[count];
							fixed (void* ptr = Clny) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "CONT":
							dataLength = count * sizeof(CONT);
							Cont = new CONT[count];
							fixed (void* ptr = Cont) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "CTZN":
							dataLength = count * sizeof(CTZN);
							Ctzn = new CTZN[count];
							fixed (void* ptr = Ctzn) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "CULT":
							dataLength = count * sizeof(CULT);
							Cult = new CULT[count];
							fixed (void* ptr = Cult) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "DIFF":
							dataLength = count * sizeof(DIFF);
							Diff = new DIFF[count];
							fixed (void* ptr = Diff) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "ERAS":
							dataLength = count * sizeof(ERAS);
							Eras = new ERAS[count];
							fixed (void* ptr = Eras) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "ESPN":
							dataLength = count * sizeof(ESPN);
							Espn = new ESPN[count];
							fixed (void* ptr = Espn) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "EXPR":
							dataLength = count * sizeof(EXPR);
							Expr = new EXPR[count];
							fixed (void* ptr = Expr) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "FLAV":
							// FLAV has two oddities compared with other sections:
							// 1. FLAV's count is not technically locked at 7, but is practically so. This means it can be treated as static (see FLAV.cs)
							count = 7;
							// 2. FLAV is the only section which is divided into section groups. However, the number of section groups is always 1,
							//   so again for practical usage, all that needs to happen is that the offset needs to be shifted an extra 4 for the extra int
							offset += 4;

							dataLength = count * sizeof(FLAV);
							Flav = new FLAV[count];
							fixed (void* ptr = Flav) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "GAME":
							dataLength = 0;
							Game = new GAME[count];
							GameCiv = new int[count][];
							GameAlliance = new int[count][];

							fixed (void* ptr = Game) {
								byte* gamePtr = (byte*)ptr;
								byte* dataPtr = bytePtr + offset;
								int rowLength = 0;
								int playableCivs = 0;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, gamePtr, GAME_LEN_1, GAME_LEN_1);
									gamePtr += GAME_LEN_1;
									playableCivs = Game[i].NumberOfPlayableCivs == 0 ? 31 : Game[i].NumberOfPlayableCivs;
									GameCiv[i] = new int[playableCivs];
									rowLength = playableCivs * sizeof(int);
									fixed (void* ptr2 = GameCiv[i]) Buffer.MemoryCopy(dataPtr + GAME_LEN_1, ptr2, rowLength, rowLength);
									dataPtr += GAME_LEN_1 + rowLength;

									Buffer.MemoryCopy(dataPtr, gamePtr, GAME_LEN_2, GAME_LEN_2);
									gamePtr += GAME_LEN_2;
									GameAlliance[i] = new int[playableCivs];
									fixed (void* ptr2 = GameAlliance[i]) Buffer.MemoryCopy(dataPtr + GAME_LEN_2, ptr2, rowLength, rowLength);
									dataPtr += GAME_LEN_2 + rowLength;

									Buffer.MemoryCopy(dataPtr, gamePtr, GAME_LEN_3, GAME_LEN_3);
									gamePtr += GAME_LEN_3;
									dataPtr += GAME_LEN_3;

									dataLength += Game[i].Length + 4;
								}
							}
							break;
						case "GOOD":
							dataLength = count * sizeof(GOOD);
							Good = new GOOD[count];
							fixed (void* ptr = Good) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "GOVT":
							int govtLen = FileData.ReadInt32(offset) + 4;
							dataLength = count * govtLen;
							Govt = new GOVT[count];
							GovtGovt = new GOVT_GOVT[count, count];
							int govtgovtRowLength = count * sizeof(GOVT_GOVT);

							fixed (void* ptr = Govt, ptr2 = GovtGovt) {
								byte* govtPtr = (byte*)ptr;
								byte* govtgovtPtr = (byte*)ptr2;
								byte* dataPtr = bytePtr + offset;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, govtPtr, GOVT_LEN_1, GOVT_LEN_1);
									Buffer.MemoryCopy(dataPtr + GOVT_LEN_1, govtgovtPtr, govtgovtRowLength, govtgovtRowLength);
									Buffer.MemoryCopy(dataPtr + GOVT_LEN_1 + govtgovtRowLength, govtPtr + GOVT_LEN_1, GOVT_LEN_2, GOVT_LEN_2);
									govtPtr += sizeof(GOVT);
									govtgovtPtr += govtgovtRowLength;
									dataPtr += govtLen;
								}
							}
							break;
						case "LEAD":
							dataLength = 0;
							Lead = new LEAD[count];
							LeadPrto = new LEAD_Unit[count][];
							LeadTech = new int[count][];

							fixed (void* ptr = Lead) {
								byte* leadPtr = (byte*)ptr;
								byte* dataPtr = bytePtr + offset;
								int rowLength = 0;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, leadPtr, LEAD_LEN_1, LEAD_LEN_1);
									leadPtr += LEAD_LEN_1;
									LeadPrto[i] = new LEAD_Unit[Lead[i].NumberOfStartUnitTypes];
									rowLength = Lead[i].NumberOfStartUnitTypes * sizeof(LEAD_Unit);
									fixed (void* ptr2 = LeadPrto[i]) Buffer.MemoryCopy(dataPtr + LEAD_LEN_1, ptr2, rowLength, rowLength);
									dataPtr += LEAD_LEN_1 + rowLength;

									Buffer.MemoryCopy(dataPtr, leadPtr, LEAD_LEN_2, LEAD_LEN_2);
									leadPtr += LEAD_LEN_2;
									LeadTech[i] = new int[Lead[i].NumberOfStartingTechnologies];
									rowLength = Lead[i].NumberOfStartingTechnologies * sizeof(int);
									fixed (void* ptr2 = LeadTech[i]) Buffer.MemoryCopy(dataPtr + LEAD_LEN_2, ptr2, rowLength, rowLength);
									dataPtr += LEAD_LEN_2 + rowLength;

									Buffer.MemoryCopy(dataPtr, leadPtr, LEAD_LEN_3, LEAD_LEN_3);
									leadPtr += LEAD_LEN_3;
									dataPtr += LEAD_LEN_3;

									dataLength += Lead[i].Length + 4;
								}
							}
							break;
						case "PRTO":
							dataLength = 0;
							Prto = new PRTO[count];
							PrtoPrto = new int[count][];
							int prtoprtoRowLength = 0;

							fixed (void* ptr = Prto) {
								byte* prtoPtr = (byte*)ptr;
								byte* dataPtr = bytePtr + offset;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, prtoPtr, PRTO_LEN_1, PRTO_LEN_1);
									PrtoPrto[i] = new int[Prto[i].NumberOfStealthTargets];
									prtoprtoRowLength = Prto[i].NumberOfStealthTargets * sizeof(int);
									fixed (void* ptr2 = PrtoPrto[i]) Buffer.MemoryCopy(dataPtr + PRTO_LEN_1, ptr2, prtoprtoRowLength, prtoprtoRowLength);
									Buffer.MemoryCopy(dataPtr + PRTO_LEN_1 + prtoprtoRowLength, prtoPtr + PRTO_LEN_1, PRTO_LEN_2, PRTO_LEN_2);

									prtoPtr += sizeof(PRTO);
									dataPtr += Prto[i].Length + 4;
									dataLength += Prto[i].Length + 4;
								}
							}
							break;
						case "RACE":
							dataLength = 0;
							Race = new RACE[count];
							RaceCityName = new RACE_City[count][];
							RaceScientificLeaderName = new RACE_LeaderName[count][];
							RaceGreatLeaderName = new RACE_LeaderName[count][];
							/*
								For getting dynamic race data, we need to know the number of eras as defined earlier
								Presumably this means that the ERAS section of BIQ will always appear before the RACE section
								However, if ERAS ever appears after RACE, then that will be a problem
								I considered throwing an exception here in that case, but C# will throw a NullReferenceException anyway for
								  trying to get the length of an uninitialized array, which is sufficient
							*/
							int eras = Eras.Length;
							RaceEra = new RACE_ERAS[count, eras];
							int raceeraRowLength = eras * sizeof(RACE_ERAS);

							fixed (void* ptr = Race, ptr2 = RaceEra) {
								byte* racePtr = (byte*)ptr;
								byte* raceeraPtr = (byte*)ptr2;
								byte* dataPtr = bytePtr + offset;
								int rowLength = 0;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_1, RACE_LEN_1);
									racePtr += RACE_LEN_1;
									RaceCityName[i] = new RACE_City[Race[i].NumberOfCities];
									rowLength = Race[i].NumberOfCities * sizeof(RACE_City);
									fixed (void* ptr3 = RaceCityName[i]) Buffer.MemoryCopy(dataPtr + RACE_LEN_1, ptr3, rowLength, rowLength);
									dataPtr += RACE_LEN_1 + rowLength;

									Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_2, RACE_LEN_2);
									racePtr += RACE_LEN_2;
									RaceGreatLeaderName[i] = new RACE_LeaderName[Race[i].NumberOfGreatLeaders];
									rowLength = Race[i].NumberOfGreatLeaders * sizeof(RACE_LeaderName);
									fixed (void* ptr3 = RaceGreatLeaderName[i]) Buffer.MemoryCopy(dataPtr + RACE_LEN_2, ptr3, rowLength, rowLength);
									dataPtr += RACE_LEN_2 + rowLength;

									Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_3, RACE_LEN_3);
									racePtr += RACE_LEN_3;
									Buffer.MemoryCopy(dataPtr + RACE_LEN_3, raceeraPtr, raceeraRowLength, raceeraRowLength);
									dataPtr += RACE_LEN_3 + raceeraRowLength;
									raceeraPtr += raceeraRowLength;

									Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_4, RACE_LEN_4);
									racePtr += RACE_LEN_4;
									RaceScientificLeaderName[i] = new RACE_LeaderName[Race[i].NumberOfScientificLeaders];
									rowLength = Race[i].NumberOfScientificLeaders * sizeof(RACE_LeaderName);
									fixed (void* ptr3 = RaceScientificLeaderName[i]) Buffer.MemoryCopy(dataPtr + RACE_LEN_4, ptr3, rowLength, rowLength);
									dataPtr += RACE_LEN_4 + rowLength;

									dataLength += Race[i].Length + 4;
								}
							}

							break;
						case "RULE":
							dataLength = 0;
							Rule = new RULE[count];
							RuleCult = new RULE_CULT[count][];
							RuleSpaceship = new int[count][];

							fixed (void* ptr = Rule) {
								byte* rulePtr = (byte*)ptr;
								byte* dataPtr = bytePtr + offset;
								int rowLength = 0;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, rulePtr, RULE_LEN_1, RULE_LEN_1);
									rulePtr += RULE_LEN_1;
									RuleSpaceship[i] = new int[Rule[i].NumberOfSpaceshipParts];
									rowLength = Rule[i].NumberOfSpaceshipParts * sizeof(int);
									fixed (void* ptr2 = RuleSpaceship[i]) Buffer.MemoryCopy(dataPtr + RULE_LEN_1, ptr2, rowLength, rowLength);
									dataPtr += RULE_LEN_1 + rowLength;

									Buffer.MemoryCopy(dataPtr, rulePtr, RULE_LEN_2, RULE_LEN_2);
									rulePtr += RULE_LEN_2;
									RuleCult[i] = new RULE_CULT[Rule[i].NumberOfCultureLevels];
									rowLength = Rule[i].NumberOfCultureLevels * sizeof(RULE_CULT);
									fixed (void* ptr2 = RuleCult[i]) Buffer.MemoryCopy(dataPtr + RULE_LEN_2, ptr2, rowLength, rowLength);
									dataPtr += RULE_LEN_2 + rowLength;

									Buffer.MemoryCopy(dataPtr, rulePtr, RULE_LEN_3, RULE_LEN_3);
									rulePtr += RULE_LEN_3;
									dataPtr += RULE_LEN_3;

									dataLength += Rule[i].Length + 4;
								}
							}
							break;
						case "SLOC":
							dataLength = count * sizeof(SLOC);
							Sloc = new SLOC[count];
							fixed (void* ptr = Sloc) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "TECH":
							dataLength = count * sizeof(TECH);
							Tech = new TECH[count];
							fixed (void* ptr = Tech) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "TERR":
							int terrLen = FileData.ReadInt32(offset) + 4; // Add 4 because length must also include the 32-bit integer that is itself
							dataLength = count * terrLen;
							Terr = new TERR[count];
							int goodCount = FileData.ReadInt32(offset + 4);
							TerrGood = new bool[count, goodCount];

							fixed (void* ptr = Terr) {
								byte* terrBytePtr = (byte*)ptr;
								byte* dataPtr = bytePtr + offset;

								// TERR contains dynamic data, so it can't be read in as a block. Instead, read in data for each TERR
								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, terrBytePtr, TERR_LEN_1, TERR_LEN_1);
									dataPtr += TERR_LEN_1;

									// Get TerrGood flags, dynamic data which determines which resources are allowed on which terrain types
									for (int j = 0; j < goodCount; j++) {
										TerrGood[i, j] = Util.GetFlag(*dataPtr, j % 8);
										// Incrememt byte position every 8th bit read or after all bits read:
										if (j % 8 == 7 || j == goodCount - 1) dataPtr++;
									}

									Buffer.MemoryCopy(dataPtr, terrBytePtr + TERR_LEN_1, TERR_LEN_2, TERR_LEN_2);
									terrBytePtr += sizeof(TERR);
									dataPtr += TERR_LEN_2;
								}
							}
							break;
						case "TFRM":
							dataLength = count * sizeof(TFRM);
							Tfrm = new TFRM[count];
							fixed (void* ptr = Tfrm) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "TILE":
							dataLength = count * sizeof(TILE);
							Tile = new TILE[count];
							fixed (void* ptr = Tile) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "UNIT":
							dataLength = count * sizeof(UNIT);
							Unit = new UNIT[count];
							fixed (void* ptr = Unit) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "WCHR":
							dataLength = count * sizeof(WCHR);
							Wchr = new WCHR[count];
							fixed (void* ptr = Wchr) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						case "WMAP":
							dataLength = 0;
							Wmap = new WMAP[count];
							WmapResource = new int[count][];
							int wmapResourceLength = 0;

							fixed (void* ptr = Wmap) {
								byte* wmapPtr = (byte*)ptr;
								byte* dataPtr = bytePtr + offset;

								for (int i = 0; i < count; i++) {
									Buffer.MemoryCopy(dataPtr, wmapPtr, WMAP_LEN_1, WMAP_LEN_1);
									WmapResource[i] = new int[Wmap[i].NumberOfResources];
									wmapResourceLength = Wmap[i].NumberOfResources * sizeof(int);
									fixed (void* ptr2 = WmapResource[i]) Buffer.MemoryCopy(dataPtr + WMAP_LEN_1, ptr2, wmapResourceLength, wmapResourceLength);
									Buffer.MemoryCopy(dataPtr + WMAP_LEN_1 + wmapResourceLength, wmapPtr + WMAP_LEN_1, WMAP_LEN_2, WMAP_LEN_2);

									wmapPtr += sizeof(WMAP);
									dataPtr += Wmap[i].Length + 4;
									dataLength += Wmap[i].Length + 4;
								}
							}
							break;
						case "WSIZ":
							dataLength = count * sizeof(WSIZ);
							Wsiz = new WSIZ[count];
							fixed (void* ptr = Wsiz) {
								Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
							}
							break;
						default:
							throw new Exception("An error occured while parsing the BIQ file because a header was not found where expected.  Instead, found " + header);
					}
					offset += dataLength;
				}
			}
		}
	}
}
