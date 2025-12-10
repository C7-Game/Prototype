using C7Engine;
using C7GameData;
using Godot;
using System.Collections.Generic;
using System.Linq;
using static C7Engine.MsgChooseResearch;
using static C7GameData.EraUtils;

public partial class ScienceAdvisor : TextureRect {
	private ImageTexture AncientBackground;
	private ImageTexture MiddleBackground;
	private ImageTexture IndustrialBackground;
	private ImageTexture ModernBackground;
	private TextureButton nextEra;
	private TextureButton previousEra;
	private List<TechBox> techBoxes = new();
	private TextureRect advisorHead = new();

	// Stored separately so we can modify this without mutating the player.
	private string eraName;

	// store the last opened era window so next time we open the advisor, it opens at the same era window
	private static string lastOpenedEra = string.Empty;

	private string advisorTitleString = "SCIENCE ADVISOR";

	private FontFile regularFont = new();
	private Theme regularBigFontTheme = new();

	private int bigFontSize = 26;
	// private int middleFontSize = 20;

	private int bigFontGlyphSpacing = 14;
	private int bigFontGlyphSpaceSpacing = 22;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.CreateUI();

		EngineStorage.ReadGameData((GameData gameData) => {
			List<Tech> allTechs = gameData.techs;
			Player player = gameData.GetFirstHumanPlayer();
			eraName = string.IsNullOrEmpty(lastOpenedEra) ? player.eraCivilopediaName : lastOpenedEra;
			this.DrawTechTree(eraName, player, allTechs, player.GetAvailableTechsToResearch(allTechs));
		});
	}

	private void CreateUI() {

		regularFont = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf");
		regularBigFontTheme.DefaultFont = regularFont;
		regularBigFontTheme.SetFontSize("font_size", "Label", bigFontSize);

		// science_industrial_new is used as the industrial tech tree is
		// different from vanilla civ3.
		AncientBackground = TextureLoader.Load("advisors.science.background.ancient");
		MiddleBackground = TextureLoader.Load("advisors.science.background.middle");
		IndustrialBackground = TextureLoader.Load("advisors.science.background.industrial");
		ModernBackground = TextureLoader.Load("advisors.science.background.modern");

		advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Science, AdvisorHead.Mood.Happy, eraIndex: 0);
		advisorHead.SetPosition(new Vector2(851, 0));
		AddChild(advisorHead);

		FontVariation fontVariation = new FontVariation
		{
			BaseFont = regularFont,
			SpacingGlyph = bigFontGlyphSpacing,
			SpacingSpace = bigFontGlyphSpaceSpacing,
		};

		Theme regularThemeWithCustomSpacing = new Theme();
		regularThemeWithCustomSpacing.SetFont("font", "Label", fontVariation);
		regularThemeWithCustomSpacing.SetFontSize("font_size", "Label", bigFontSize);

		float containerWidth = AncientBackground.GetWidth();

		float advisorTitleStringWidth = GetStringSizeWithCustomSpacing(regularFont, advisorTitleString, bigFontSize,
			bigFontGlyphSpacing, bigFontGlyphSpaceSpacing).X;

		float advisorTitleOffsetLeft = (containerWidth / 2.0f) - (advisorTitleStringWidth) / 2.0f;

		Label advisorTitle = new() {
			Text = advisorTitleString,
			OffsetLeft = advisorTitleOffsetLeft,
			OffsetTop = 15,
			Theme = regularThemeWithCustomSpacing,
		};
		AddChild(advisorTitle);


		ImageTexture DialogBoxTexture = TextureLoader.Load("advisors.dialog_box");
		TextureButton DialogBox = new TextureButton();
		DialogBox.TextureNormal = DialogBoxTexture;
		DialogBox.SetPosition(new Vector2(806, 110));
		AddChild(DialogBox);

		//TODO: Multi-line capabilities
		Label DialogBoxAdvise = new Label();
		DialogBoxAdvise.Text = "You are running OpenCiv3!";
		DialogBoxAdvise.SetPosition(new Vector2(815, 119));
		AddChild(DialogBoxAdvise);

		ImageTexture GoBackTexture = TextureLoader.Load("ui.exit.normal");
		TextureButton GoBackButton = new TextureButton();
		GoBackButton.TextureNormal = GoBackTexture;
		GoBackButton.SetPosition(new Vector2(952, 720));
		AddChild(GoBackButton);
		GoBackButton.Pressed += ReturnToMenu;

		previousEra = new();
		TextureLoader.SetButtonTextures(previousEra, "advisors.science.navigation.button");
		previousEra.SetPosition(new Vector2(512 - 128 - 100, 720));
		AddChild(previousEra);
		previousEra.Pressed += () => { ChangeEraAndDrawTree(-1); };

		TextureRect leftArrow = new() {
			Texture = TextureLoader.Load("advisors.science.navigation.arrow_previous")
		};
		previousEra.AddChild(leftArrow);
		leftArrow.SetPosition(new Vector2(-44, 13));

		Label previousEraLabel = new();
		previousEra.AddChild(previousEraLabel);
		previousEraLabel.SetTextAndCenterLabel("Previous Era");
		previousEraLabel.Position += new Vector2(0, 7);

		nextEra = new();
		TextureLoader.SetButtonTextures(nextEra, "advisors.science.navigation.button");
		nextEra.SetPosition(new Vector2(512 + 100, 720));
		AddChild(nextEra);
		nextEra.Pressed += () => { ChangeEraAndDrawTree(1); };

		TextureRect rightArrow = new() {
			Texture = TextureLoader.Load("advisors.science.navigation.arrow_next")
		};
		nextEra.AddChild(rightArrow);
		rightArrow.SetPosition(new Vector2(129, 13));

		Label nextEraLabel = new();
		nextEra.AddChild(nextEraLabel);
		nextEraLabel.SetTextAndCenterLabel("Next Era");
		nextEraLabel.Position += new Vector2(0, 7);
	}

	void DrawTechTree(string eraName, Player player, List<Tech> allTechs, HashSet<Tech> availableTechsToResearch) {
		HashSet<ID> knownTechs = player.knownTechs;
		previousEra.Show();
		nextEra.Show();

		lastOpenedEra = eraName;

		Queue<Tech> queue = player.ResearchQueue;

		// Set the tech background based on the player's era.
		if (eraName == ANCIENT_TIMES_CVLPD) {
			previousEra.Hide();
			this.Texture = AncientBackground;
		} else if (eraName == MIDDLE_AGES_CVLPD) {
			this.Texture = MiddleBackground;
		} else if (eraName == INDUSTRIAL_AGE_CVLPD) {
			this.Texture = IndustrialBackground;
		} else if (eraName == MODERN_ERA_CVLPD) {
			this.Texture = ModernBackground;
			nextEra.Hide();
		}
		advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Science, AdvisorHead.Mood.Happy, player.EraIndex());

		foreach (Tech tech in allTechs) {
			if (tech.EraCivilopediaName != eraName) {
				continue;
			}

			TechBox.TechState techState = TechBox.TechState.kBlocked;
			if (knownTechs.Contains(tech.id)) {
				techState = TechBox.TechState.kKnown;
			} else if (player.currentlyResearchedTech == tech.id) {
				techState = TechBox.TechState.kInProgress;
			} else if (queue.Count > 0 && queue.Contains(tech)) {
				techState = TechBox.TechState.kQueued;
			} else if (availableTechsToResearch.Contains(tech)) {
				techState = TechBox.TechState.kPossible;
			} else {
				techState = TechBox.TechState.kBlocked;
			}

			int queueNumber = queue.ToList().IndexOf(tech) + 1;

			TechBox techButton = new(tech, techState, queueNumber);
			techButton.SetPosition(new Vector2(tech.X, tech.Y));
			techButton.Pressed += () => {
				SelectionMode selection = Input.IsKeyPressed(Key.Shift) ? SelectionMode.Multi : SelectionMode.Single;
				new MsgChooseResearch(tech, AdvisorState.Show, selection).send();
			};
			AddChild(techButton);
			techBoxes.Add(techButton);
		}
	}

	private void ChangeEraAndDrawTree(int delta) {
		foreach (TechBox tb in techBoxes) {
			RemoveChild(tb);
			tb.QueueFree();
		}
		techBoxes.Clear();

		EngineStorage.ReadGameData((GameData gameData) => {
			List<Tech> allTechs = gameData.techs;
			Player player = gameData.GetFirstHumanPlayer();
			eraName = string.IsNullOrEmpty(lastOpenedEra)
				? EraIndexToEra(GetEraIndex(eraName) + delta)
				: EraIndexToEra(GetEraIndex(lastOpenedEra) + delta);
			DrawTechTree(eraName, player, allTechs, player.GetAvailableTechsToResearch(allTechs));
		});
	}

	private void ReturnToMenu() {
		GetParent<Advisors>().Hide();
	}

	private Vector2 GetStringSizeWithCustomSpacing(Font font, string input, int fontSize = 16, int glyphSpacing = 0, int glyphSpaceSpacing = 0) {

		float extraSpacing = 0.0f;
		for (int i = 0; i < input.Length; i++) {
			if (i < input.Length - 1) {
				if (char.IsWhiteSpace(input[i]) && glyphSpaceSpacing > 0) {
					extraSpacing += glyphSpaceSpacing;
				} else {
					extraSpacing += glyphSpacing;
				}
			}
		}

		Vector2 originalSize = font.GetStringSize(input, fontSize: fontSize);
		return new Vector2(originalSize.X + extraSpacing, originalSize.Y);
	}
}
