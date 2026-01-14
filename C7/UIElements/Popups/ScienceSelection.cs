using Godot;
using C7GameData;
using C7Engine;
using System.Collections.Generic;
using static C7Engine.MsgChooseResearch;

public partial class ScienceSelection : Popup {
	Player player;
	List<Tech> options = new();

	public ScienceSelection(Player player) {
		alignment = BoxContainer.AlignmentMode.End;
		margins = new Margins(right: 10);
		this.player = player;
	}

	public override void _Ready() {
		base._Ready();

		int width = 390;
		int height = 350;

		TextureRect advisorHead = new();
		advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Science, AdvisorHead.Mood.Happy, player.EraIndex());
		advisorHead.SetPosition(new Vector2(235, 0));
		AddChild(advisorHead);

		AddTexture(width, height);
		AddBackground(width, height - 110, 110);
		AddHeader("Science Advisor", 120);

		Label messageLabel = new();
		messageLabel.Text = "What shall we explore now?";
		messageLabel.SetPosition(new Vector2(25, 160));
		AddChild(messageLabel);

		OptionButton optionButton = MakeStyledOptionButton();
		AddChild(optionButton);
		optionButton.SetPosition(new Vector2(25, 190));

		AddButton("OK. Sounds good.", 235, () => {
			new MsgChooseResearch(options[optionButton.Selected], AdvisorState.DontShow).send();
			GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
		});
		AddButton("What's the big picture?", 265, () => {
			new MsgShowScienceAdvisor().send();
			GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
		});

		AddConfirmButton(new Vector2(width - 40, height - 40), () => {
			new MsgChooseResearch(options[optionButton.Selected], AdvisorState.DontShow).send();
			GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
		});
	}

	private OptionButton MakeStyledOptionButton() {
		OptionButton optionButton = new();
		Color borderGrey = Color.Color8(50, 50, 50, 220);
		StyleBoxFlat styleBox = new() {
			BorderColor = borderGrey,
			BorderWidthBottom = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			BorderWidthTop = 2,
			ContentMarginLeft = 4,
			ContentMarginRight = 4,
			ContentMarginTop = 4,
			ContentMarginBottom = 4
		};
		optionButton.AddThemeStyleboxOverride("normal", styleBox);
		optionButton.AddThemeStyleboxOverride("hover", styleBox); ;
		optionButton.AddThemeStyleboxOverride("pressed", styleBox);
		PopupMenu popup = optionButton.GetPopup();
		popup.AddThemeStyleboxOverride("panel", styleBox);

		EngineStorage.ReadGameData((GameData gameData) => {
			// first suggest the next item in the queue
			if (player.ResearchQueue.Count > 0) {
				AddItem(gameData, player.ResearchQueue.Peek(), optionButton);
			}
			// then the rest
			foreach (Tech tech in player.GetAvailableTechsToResearch(gameData.techs)) {
				if (!options.Contains(tech)) {
					AddItem(gameData, tech, optionButton);
				}
			}

			for (int i = 0; i < popup.ItemCount; ++i) {
				popup.SetItemAsRadioCheckable(i, false);
				popup.SetItemAsCheckable(i, false);
			}
		});

		return optionButton;
	}

	private void AddItem(GameData gameData, Tech tech, OptionButton optionButton) {
		int turns = player.EstimateTurnsToResearch(gameData, tech);
		string turnsStr = turns == int.MaxValue ? "--" : $"{turns}";
		optionButton.AddItem($"{tech.Name} ({turnsStr} turns)");
		options.Add(tech);
	}
}
