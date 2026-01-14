using C7Engine;
using C7GameData;
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using ConvertCiv3Media;

public partial class TradeOfferUi : Tree {
	TreeItem peaceTreaty;
	TreeItem lumpSumGold;
	List<TreeItem> techs = new();
	TradeOffer currentOffer;
	List<Tech> tradeableTechs;
	int playerGold;

	public TradeOfferUi(Theme fontTheme, List<Tech> tradeableTechs, int playerGold,
						TradeOffer currentOffer, bool requiresPeaceTreaty,
						HorizontalAlignment alignment) {
		this.currentOffer = currentOffer;
		this.tradeableTechs = tradeableTechs;
		this.playerGold = playerGold;
		this.Columns = 1;
		this.AllowRmbSelect = true;
		TradingTree.ConfigureTreeTheme(this, fontTheme);
		TreeItem root = TradingTree.CreateTreeRoot(this);

		// Match the size of the texture used in the deal screen.
		this.Size = new Vector2(190, 100);

		if (requiresPeaceTreaty) {
			peaceTreaty = this.CreateItem(root);
			peaceTreaty.SetTextAlignment(0, alignment);
			peaceTreaty.SetText(0, "Peace Treaty");
		}

		// Add tree items for all the possible things that could be traded - we
		// determine whether they're visible based on the current offer in
		// RefreshUiForOffer.
		lumpSumGold = this.CreateItem(root);
		lumpSumGold.SetTextAlignment(0, alignment);

		foreach (Tech tech in tradeableTechs) {
			TreeItem child = this.CreateItem(root);
			child.SetTextAlignment(0, alignment);
			child.SetText(0, tech.Name);
			techs.Add(child);
		}

		RefreshUiForOffer();
	}

	public void HandleClicks(TradeOfferUi other, Action offerUpdated) {
		this.ItemMouseSelected += (Vector2 mousePos, long mouseButtonIndex) => {
			TreeItem ti = this.GetSelected();
			ti.Deselect(0);

			// Handle techs being clicked on.
			if (techs.Contains(ti)) {
				Tech t = tradeableTechs.Find(x => x.Name == ti.GetText(0));
				currentOffer.techs.Remove(t);
			}
			if (ti == lumpSumGold && mouseButtonIndex != 2) {
				currentOffer.gold = null;
			}

			// Allow right clicking the gold amount to change it.
			if (ti == lumpSumGold && mouseButtonIndex == 2) {
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
											"Gold: ", "" + currentOffer.gold.Value,
											BoxContainer.AlignmentMode.Center,
											handleTextInput),
								PopupOverlay.PopupCategory.Advisor);
			}

			if (ti == peaceTreaty) {
				// If we click the peace treaty off the trading table, remove
				// everything else too.
				if (currentOffer.partOfPeaceTreaty) {
					currentOffer.Clear();
					other.currentOffer.Clear();
				} else {
					currentOffer.partOfPeaceTreaty = true;
				}
			}

			offerUpdated();
			RefreshUiForOffer();
		};
	}

	public void RefreshUiForOffer() {
		if (currentOffer.gold.HasValue) {
			lumpSumGold.SetText(0, $"{currentOffer.gold.Value} gold");
			lumpSumGold.Visible = true;
		} else {
			lumpSumGold.Visible = false;
		}

		foreach (TreeItem ti in techs) {
			ti.Visible = currentOffer.techs.Any(x => x.Name == ti.GetText(0));
		}

		if (peaceTreaty != null) {
			peaceTreaty.Visible = currentOffer.partOfPeaceTreaty;
		}
	}
}
