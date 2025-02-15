using Godot;
using System;
using Serilog;
using System.Collections.Generic;
using C7GameData;
using C7.Map;
using C7Engine;
using C7Engine.AI;


// Handles the city screen, where citizens can be assigned and other details of
// the city can bee seen.
public partial class CityScreen : CenterContainer {
	private ILogger log = LogManager.ForContext<CityScreen>();
	public TileAssignmentLayer tileAssignmentLayer;
	public MapView mapView;
	private TextureRect background;
	private List<TextureButton> popHeads = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		background = new() {
			Texture = Util.LoadTextureFromPCX("Art/city screen/background.pcx")
		};
		AddChild(background);

		TextureButton close = new() {
			TextureNormal = Util.LoadTextureFromPCX("Art/city screen/cityMgmtButtons.pcx", 155, 1, 32, 48),
			TextureHover = Util.LoadTextureFromPCX("Art/city screen/cityMgmtButtons.pcx", 155, 50, 32, 48),
			TexturePressed = Util.LoadTextureFromPCX("Art/city screen/cityMgmtButtons.pcx", 155, 99, 32, 48)
		};
		close.SetPosition(new Vector2(950, 20));
		close.Pressed += () => { HideScreen(); };
		background.AddChild(close);

		this.Hide();
	}

	public override void _UnhandledInput(InputEvent @event) {
		// Only capture mouse events if we're visible.
		if (!this.Visible) {
			return;
		}

		// If we left clicked on a tile, handle reassigning a citizen accordingly.
		if (@event is InputEventMouseButton eventMouseButton) {
			if (eventMouseButton.ButtonIndex == MouseButton.Left) {
				GetViewport().SetInputAsHandled();
				if (eventMouseButton.IsPressed()) {
					using (UIGameDataAccess gameDataAccess = new()) {
						Tile tile = mapView.tileOnScreenAt(gameDataAccess.gameData.map, eventMouseButton.Position);
						if (tile != null) {
							HandleReassignment(tile, gameDataAccess.gameData.citizenTypes);
							RenderPopHeads(tileAssignmentLayer.city);
						}
					}
				}
			}
		}
	}

	private void HandleReassignment(Tile tile, List<CitizenType> citizenTypes) {
		City city = tileAssignmentLayer.city;

		// We can't assign citizens to other cities.
		if (tile.cityAtTile != null && tile.cityAtTile != city) {
			return;
		}

		// We can't assign citizens to tiles worked by other cities.
		if (tile.personWorkingTile != null && tile.personWorkingTile.city != city) {
			return;
		}

		// If we're already working a tile and click on it, turn the citizen
		// into an entertainer.
		if (tile.personWorkingTile != null && tile.personWorkingTile.city == city) {
			// TODO: implement entertainers.
			return;
		}

		// We've clicked on an unworked tile, move the "worst" citizen to that
		// tile.
		//
		// TODO: only allow this within the city's borders/BFC.
		if (tile.cityAtTile == null) {
			int worstYield = int.MaxValue;
			CityResident worst = null;

			foreach (CityResident cr in city.residents) {
				int tileYield = cr.tileWorked.foodYield(city.owner) +
								cr.tileWorked.productionYield(city.owner) +
								cr.tileWorked.commerceYield(city.owner);
				if (tileYield < worstYield) {
					worstYield = tileYield;
					worst = cr;
				}
			}

			// Move the worst citizen to our new tile, being sure to update
			// backpointers from the tile.
			worst.tileWorked.personWorkingTile = null;
			worst.tileWorked = tile;
			tile.personWorkingTile = worst;
			worst.citizenType = citizenTypes.Find(x => x.IsDefaultCitizen);
			return;
		}

		// If we've clicked the city center, re-assign all the citizens using
		// the basic AI.
		//
		// TODO: This throws away existing nationalities, fix that.
		if (tile.cityAtTile == city) {
			int numResidents = city.residents.Count;
			city.RemoveAllCitizens();

			for (int i = 0; i < numResidents; ++i) {
				CityResident newResident = new() {
					citizenType = citizenTypes.Find(x => x.IsDefaultCitizen),
					nationality = city.owner.civilization,
					city = city
				};
				CityTileAssignmentAI.AssignNewCitizenToTile(newResident);
			}
		}
	}

	public void HideScreen() {
		this.Hide();
		tileAssignmentLayer.city = null;
	}

	private void OnShowCityScreen(ParameterWrapper<City> city) {
		this.Show();
		tileAssignmentLayer.city = city.Value;
		RenderPopHeads(city.Value);
	}

	private void RenderPopHeads(City city) {
		// Reset any old heads.
		foreach (TextureButton head in popHeads) {
			background.RemoveChild(head);
			head.QueueFree();
		}
		popHeads.Clear();

		int eraNum = 0;
		if (city.owner.eraCivilopediaName == "ERAS_Ancient_Times") {
			eraNum = 0;
		} else if (city.owner.eraCivilopediaName == "ERAS_Middle_Ages") {
			eraNum = 1;
		} else if (city.owner.eraCivilopediaName == "ERAS_Industrial_Age") {
			eraNum = 2;
		} else if (city.owner.eraCivilopediaName == "ERAS_Modern_Era") {
			eraNum = 3;
		}

		// The pop head textures are 50 x 50, but have a 1px border on all sides
		//
		// The texture file has 16 rows of the default citizen, in groups of 4
		// per era (content, happy, resisting, unhappy). There are 10 columns,
		// the first 5 are male heads of different regions for civs, the other 5
		// are female heads.
		//
		// After the 16 rows of default citizens there is one row per specialist
		// type, and again 10 columns per row. This time they are
		// (ancient, middle, industrial, modern, blank) for male and female heads
		//
		// TODO: handle citizen moods
		// TODO: handle per-civ regions
		// TODO: handle male/female citizens

		// Start by splitting the default residents from the specialists, since
		// they are spaced apart in the UI.
		List<CityResident> defaultResidents = city.residents.FindAll(x => x.citizenType.IsDefaultCitizen);
		List<CityResident> specialists = city.residents.FindAll(x => !x.citizenType.IsDefaultCitizen);

		// Each head is 48px, so leave a 1 head gap if we have specialists.
		int width = city.residents.Count * 48;
		if (specialists.Count > 0) {
			width += 48;
		}

		// Track the x position of each head so that we're centered in the screen
		int xPos = background.Texture.GetWidth() / 2 + -width / 2;

		// Add each of the default citizens. These are buttons with the idea that
		// we can eventually support clicking on the heads to view details, such
		// as the reason for unhappiness.
		foreach (CityResident cr in defaultResidents) {
			TextureButton tb = new();
			tb.TextureNormal = Util.LoadTextureFromPCX("Art/SmallHeads/popHeads.pcx",
														0 + 1, 200 * eraNum + 1, 48, 48);
			tb.SetPosition(new Vector2(xPos, 440));
			background.AddChild(tb);
			popHeads.Add(tb);
			xPos += 48;
		}

		// Add space before specialists.
		xPos += 48;

		// Add each of the specialists.
		//
		// TODO: When clicking a specialist, have it iterate through the types
		// of specialists known to the player.
		//
		// TODO: Render the specialist effect (like a smiley for entertainers)
		// in the corner of the head.
		foreach (CityResident cr in specialists) {
			TextureButton tb = new();
			int textX = 50 * eraNum;
			int numRowsOfLaborers = 16;
			int textY = 50 * numRowsOfLaborers + 50 * (cr.citizenType.SpecialistIndex - 1);
			tb.TextureNormal = Util.LoadTextureFromPCX("Art/SmallHeads/popHeads.pcx",
														textX + 1, textY + 1, 48, 48);
			tb.SetPosition(new Vector2(xPos, 440));
			background.AddChild(tb);
			popHeads.Add(tb);
			xPos += 48;
		}
	}
}
