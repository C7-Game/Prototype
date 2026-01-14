using C7Engine;
using System.Linq;
using C7GameData;
using Godot;
using System;
using System.Collections.Generic;
using ConvertCiv3Media;

public partial class TradingTree : Tree {
	TreeItem goldHeader;
	TreeItem techHeader;
	TreeItem lumpSum;
	List<TreeItem> techItems = new();
	TreeItem diplomacyHeader;
	TreeItem peaceTreaty;

	List<Tech> tradeableTechs;
	TradeOffer currentOffer;
	int playerGold;

	public TradingTree(Theme fontTheme,
						int playerGold,
						List<Tech> tradeableTechs,
						TradeOffer currentOffer, bool requiresPeaceTreaty) {
		this.tradeableTechs = tradeableTechs;
		this.currentOffer = currentOffer;
		this.playerGold = playerGold;
		this.Columns = 1;

		ConfigureTreeTheme(this, fontTheme);
		TreeItem root = CreateTreeRoot(this);

		// Match the size of the texture used in the deal screen.
		this.Size = new Vector2(190, 400);

		if (requiresPeaceTreaty) {
			diplomacyHeader = this.CreateItem(root);
			diplomacyHeader.SetText(0, "Diplomatic Agreements");
			diplomacyHeader.Collapsed = false;

			peaceTreaty = this.CreateItem(diplomacyHeader);
			peaceTreaty.SetText(0, "Peace Treaty");

			// TODO: right of passage, mutual protection pacts, ect.
		}

		// Gold
		{
			goldHeader = this.CreateItem(root);
			goldHeader.SetText(0, $"Gold ({playerGold} in treasury)");
			goldHeader.Collapsed = true;

			// TODO: per-turn gold

			lumpSum = this.CreateItem(goldHeader);
			lumpSum.SetText(0, "Lump sum");
		}

		if (tradeableTechs.Count > 0) {
			techHeader = this.CreateItem(root);
			techHeader.SetText(0, "Technology");
			techHeader.Collapsed = true;

			foreach (Tech tech in tradeableTechs) {
				TreeItem child = this.CreateItem(techHeader);
				child.SetText(0, tech.Name);
				techItems.Add(child);
			}
		}

		RefreshUiForOffer();
	}

	public void HandleClicks(TradingTree other, Action offerUpdated) {
		this.ItemSelected += () => {
			TreeItem ti = this.GetSelected();

			// Implement header collapsing.
			ti.Collapsed = !ti.Collapsed;
			ti.Deselect(0);

			// Ensure that we are synced with the folding of the other tree.
			if (ti == goldHeader && other.goldHeader != null) {
				other.goldHeader.Collapsed = goldHeader.Collapsed;
			}
			if (ti == techHeader && other.techHeader != null) {
				other.techHeader.Collapsed = techHeader.Collapsed;
			}
			if (ti == diplomacyHeader && other.diplomacyHeader != null) {
				other.diplomacyHeader.Collapsed = diplomacyHeader.Collapsed;
			}

			// Handle techs being clicked on.
			if (ti.GetParent() == techHeader) {
				Tech t = tradeableTechs.Find(x => x.Name == ti.GetText(0));
				currentOffer.techs.Add(t);
			}

			// Allow user input for gold amounts.
			if (ti == lumpSum) {
				var handleTextInput = (string input) => {
					int gold = 0;
					bool parsed = int.TryParse(input, out gold);
					if (!parsed) {
						return;
					}
					if (gold > playerGold || gold <= 0) {
						GetParent<DealScreen>().GetParent<Diplomacy>().popupOverlay
							.ShowPopup(new InformationalPopup("Insufficient gold"),
									   PopupOverlay.PopupCategory.Advisor);
						return;
					}

					currentOffer.gold = gold;
					offerUpdated();
					RefreshUiForOffer();
				};

				GetParent<DealScreen>().GetParent<Diplomacy>().popupOverlay
					.ShowPopup(new TextDialog("Enter amount...",
											"Gold: ", "0",
											BoxContainer.AlignmentMode.Center,
											handleTextInput),
								PopupOverlay.PopupCategory.Advisor);
			}

			if (ti == peaceTreaty) {
				currentOffer.partOfPeaceTreaty = true;
				other.currentOffer.partOfPeaceTreaty = true;
			}

			offerUpdated();
			RefreshUiForOffer();
		};
	}

	public void RefreshUiForOffer() {
		bool needsPeaceTreaty = peaceTreaty != null && !currentOffer.partOfPeaceTreaty;

		if (needsPeaceTreaty) {
			goldHeader.Visible = false;
			peaceTreaty.Visible = true;

			if (techHeader != null) {
				techHeader.Visible = false;
			}
		} else {
			goldHeader.Visible = true;
			lumpSum.Visible = !currentOffer.gold.HasValue;

			if (techHeader != null) {
				techHeader.Visible = true;
			}

			foreach (TreeItem ti in techItems) {
				ti.Visible = !currentOffer.techs.Any(x => x.Name == ti.GetText(0));
			}

			if (peaceTreaty != null) {
				peaceTreaty.Visible = false;
			}
		}
	}

	public static TreeItem CreateTreeRoot(Tree tree) {
		// All trees have one root, but we don't want a single root, so hide it.
		TreeItem root = tree.CreateItem();
		tree.HideRoot = true;
		tree.HideFolding = true;

		return root;
	}

	// The central styling for the trading tree and trade offer ui trees.
	public static void ConfigureTreeTheme(Tree tree, Theme fontTheme) {
		// Configure the tree to look like we expect it to in civ - a transparent
		// background with no lines between items.
		Color transparent = new Color(0, 0, 0, 0);
		tree.Theme = fontTheme;
		StyleBoxFlat styleBox = (StyleBoxFlat)tree.GetThemeStylebox("bg", "Tree");
		styleBox.BgColor = transparent;
		styleBox.BorderWidthLeft = 0;
		styleBox.BorderWidthRight = 0;
		styleBox.BorderWidthTop = 0;
		styleBox.BorderWidthBottom = 0;
		tree.AddThemeStyleboxOverride("panel", styleBox);
		tree.AddThemeStyleboxOverride("focus", styleBox);
		tree.AddThemeColorOverride("font_color", Colors.Black);
		tree.AddThemeColorOverride("children_hl_line_color", transparent);
		tree.AddThemeColorOverride("parent_hl_line_color", transparent);
		tree.AddThemeColorOverride("relationship_line_color", transparent);
		tree.AddThemeColorOverride("guide_color", transparent);
		tree.AddThemeConstantOverride("v_separation", 1);
	}
}
