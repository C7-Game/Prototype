using Godot;
using System;
using System.Diagnostics;
using C7GameData;
using Serilog;

public partial class DisbandConfirmation : Popup {
	private ILogger log = LogManager.ForContext<DisbandConfirmation>();

	string unitType = "";

	Stopwatch loadTimer = new Stopwatch();

	//So Godot doesn't print error " Cannot construct temporary MonoObject because the class does not define a parameterless constructor"
	//Not sure how important that is *shrug*
	public DisbandConfirmation() { }

	public DisbandConfirmation(MapUnit unit) {
		alignment = BoxContainer.AlignmentMode.End;
		margins = new Margins(right: 10);
		unitType = unit.unitType.name;
	}

	public override void _EnterTree() {
		loadTimer.Start();
	}

	public override void _Ready() {
		base._Ready();

		// Dimensions in-game are 530x320
		// 110px top margin are for the advisor leaderhead, Domestic in this case.
		// For some reason it uses the Happy graphics.

		// Create a transparent texture background of the appropriate size.
		// This is super important as if we just add the children, the parent won't be able to figure
		// out the size of this TextureRect, and it won't be able to align it properly.
		AddTexture(530, 320);

		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupDOMESTIC.pcx", 1, 40, 149, 110, false);
		TextureRect AdvisorHead = new TextureRect();
		AdvisorHead.Texture = AdvisorHappy;
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		AdvisorHead.SetPosition(new Vector2(375, 0));
		AddChild(AdvisorHead);

		//TODO: Almost all (135 ms) of the disband confirmation creation, after the first time, is in the
		//background creation.  90 ms or so on the first time is before this point (and is well-cached later on).
		//Since we'll be creating the background for a lot of popups, we should optimize this.
		//Also confirmed the blitting in drawRowOfPopup is essentially free.
		AddBackground(530, 210, 110);

		AddHeader("Domestic Advisor", 120);

		Label warningMessage = new Label();
		//TODO: General-purpose text breaking up util.  Instead of \n
		//This appears to be the way to do multi line labels, see: https://godotengine.org/qa/11126/how-to-break-line-on-the-label-using-gdscript
		//Maybe there's an awesomer control we can user instead
		warningMessage.Text = "Disband " + unitType + "?  Pardon me but these are OUR people. Do \nyou really want to disband them?";

		warningMessage.SetPosition(new Vector2(25, 170));
		AddChild(warningMessage);

		AddButton("Yes, we need to!", 215, disband);
		AddButton("No. Maybe you are right, advisor.", 245, cancel);

		loadTimer.Stop();
		TimeSpan stopwatchElapsed = loadTimer.Elapsed;
		log.Verbose("Disband popup load time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");
	}

	private void disband() {
		//tell the game to disband it.  right now we're doing that first, which is WRONG!
		GetParent().EmitSignal(PopupOverlay.SignalName.UnitDisbanded);
		GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
	}

	private void cancel() {
		GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
	}
}
