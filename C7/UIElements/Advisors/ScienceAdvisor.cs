using C7Engine;
using C7GameData;
using Godot;
using System;
using System.Collections.Generic;

public partial class ScienceAdvisor : TextureRect {
	private ImageTexture AncientBackground;
	private ImageTexture MiddleBackground;
	private ImageTexture IndustrialBackground;
	private ImageTexture ModernBackground;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.CreateUI();
		this.DrawTechTree();
	}

	private void CreateUI() {
		// TODO: support other eras. Amusingly the dependency arrows are drawn
		// on this texture, so to render prereqs we need to update this per era.
		//
		// science_industrial_new is used as the industrial tech tree is
		// different from vanilla civ3.
		AncientBackground = Util.LoadTextureFromPCX("Art/Advisors/science_ancient.pcx");
		MiddleBackground = Util.LoadTextureFromPCX("Art/Advisors/science_middle.pcx");
		IndustrialBackground = Util.LoadTextureFromPCX("Art/Advisors/science_industrial_new.pcx");
		ModernBackground = Util.LoadTextureFromPCX("Art/Advisors/science_modern.pcx");

		// TODO: Age-based background.  Only use Ancient for now.
		// TODO: Consider moving this to an advisor utility, since we're copying
		// these X,Y coordinates in multiple places.
		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupSCIENCE.pcx", 1, 40, 149, 110);
		ImageTexture AdvisorAngry = Util.LoadTextureFromPCX("Art/SmallHeads/popupSCIENCE.pcx", 151, 40, 149, 110);
		ImageTexture AdvisorSad = Util.LoadTextureFromPCX("Art/SmallHeads/popupSCIENCE.pcx", 301, 40, 149, 110);
		ImageTexture AdvisorSurprised = Util.LoadTextureFromPCX("Art/SmallHeads/popupSCIENCE.pcx", 451, 40, 149, 110);

		TextureRect AdvisorHead = new();
		//TODO: Randomize or set logically
		AdvisorHead.Texture = AdvisorSurprised;
		AdvisorHead.SetPosition(new Vector2(851, 0));
		AddChild(AdvisorHead);

		ImageTexture DialogBoxTexture = Util.LoadTextureFromPCX("Art/Advisors/dialogbox.pcx");
		TextureButton DialogBox = new TextureButton();
		DialogBox.TextureNormal = DialogBoxTexture;
		DialogBox.SetPosition(new Vector2(806, 110));
		AddChild(DialogBox);

		//TODO: Multi-line capabilities
		Label DialogBoxAdvise = new Label();
		DialogBoxAdvise.Text = "You are running C7!";
		DialogBoxAdvise.SetPosition(new Vector2(815, 119));
		AddChild(DialogBoxAdvise);

		ImageTexture GoBackTexture = Util.LoadTextureFromPCX("Art/exitBox-backgroundStates.pcx", 0, 0, 72, 48);
		TextureButton GoBackButton = new TextureButton();
		GoBackButton.TextureNormal = GoBackTexture;
		GoBackButton.SetPosition(new Vector2(952, 720));
		AddChild(GoBackButton);
		GoBackButton.Pressed += ReturnToMenu;
	}

	void DrawTechTree() {
		using (UIGameDataAccess gameDataAccess = new()) {
			List<Tech> techs = gameDataAccess.gameData.techs;
			Player player = gameDataAccess.gameData.GetHumanPlayers()[0];
			HashSet<ID> knownTechs = player.knownTechs;

			// Set the tech background based on the player's era.
			if (player.eraCivilopediaName == "ERAS_Ancient_Times") {
				this.Texture = AncientBackground;
			} else if (player.eraCivilopediaName == "ERAS_Middle_Ages") {
				this.Texture = MiddleBackground;
			} else if (player.eraCivilopediaName == "ERAS_Industrial_Age") {
				this.Texture = IndustrialBackground;
			} else if (player.eraCivilopediaName == "ERAS_Modern_Era") {
				this.Texture = ModernBackground;
			}

			foreach (Tech tech in techs) {
				if (tech.EraCivilopediaName != player.eraCivilopediaName) {
					continue;
				}

				TechBox.TechState techState = TechBox.TechState.kBlocked;
				if (knownTechs.Contains(tech.id)) {
					techState = TechBox.TechState.kKnown;
				} else if (player.currentlyResearchedTech == tech.id) {
					techState = TechBox.TechState.kInProgress;
				} else {
					bool prereqsKnown = true;
					foreach (Tech prereq in tech.Prerequisites) {
						if (!knownTechs.Contains(prereq.id)) {
							prereqsKnown = false;
							break;
						}
					}
					techState = prereqsKnown ? TechBox.TechState.kPossible : TechBox.TechState.kBlocked;
				}

				TechBox techButton = new(tech, techState);
				techButton.SetPosition(new Vector2(tech.X, tech.Y));
				techButton.Pressed += () => {
					new MsgChooseResearch(tech.id).send();
				};
				AddChild(techButton);
			}
		}
	}

	private void ReturnToMenu() {
		GetParent<Advisors>().Hide();
	}
}
