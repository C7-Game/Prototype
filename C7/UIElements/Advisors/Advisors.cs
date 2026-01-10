using Godot;
using Serilog;
using System.Linq;

/**
 * Handles managing the advisor screens.
 * Showing them, hiding them... maybe some other things eventually.
 * This is part of the effort to de-centralize from Game.cs and be more event driven.
 */
public partial class Advisors : CenterContainer {
	public enum Type {
		Domestic,
		Military,
		Scientific,
	}

	private ILogger log = LogManager.ForContext<Advisors>();

	[Export] public DomesticAdvisor domesticAdvisor;
	private MilitaryAdvisor militaryAdvisor;
	private ScienceAdvisor scienceAdvisor;

	private Type latestShown = Type.Domestic;

	public override void _Ready() {
		this.Hide();
	}

	public void ShowLatestAdvisor() {
		log.Debug("Received request to show latest advisor");

		ShowAdvisor(latestShown);
	}

	public void ShowAdvisor(Type advisorType) {
		latestShown = advisorType;

		this.Show();

		foreach (TextureRect textureRect in GetChildren().OfType<TextureRect>()) {
			textureRect.Hide();
		}

		switch (advisorType) {
			case Type.Domestic: {
					domesticAdvisor.ShowAdvisor();
					break;
				}
			case Type.Military: {
					if (militaryAdvisor != null) {
						RemoveChild(militaryAdvisor);
					}

					militaryAdvisor = new MilitaryAdvisor();
					AddChild(militaryAdvisor);
					break;
				}
			case Type.Scientific: {
					// TODO: What's the best way to refresh the tech tree UI without
					// adding too many children?
					if (scienceAdvisor != null) {
						RemoveChild(scienceAdvisor);
					}

					scienceAdvisor = new ScienceAdvisor();
					AddChild(scienceAdvisor);
					break;
				}
			default:
				break;
		}
	}
}
