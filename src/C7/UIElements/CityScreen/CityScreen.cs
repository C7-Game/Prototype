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
	public List<CitizenType> citizenTypes;
	private TextureRect background;
	private List<TextureButton> popHeads = new();
	private Label culturePerTurn;
	private Label totalCulture;

	Theme yieldDetailsFontTheme = new();
	FontFile yieldDetailsFont = new();

	private Label foodDetails;
	private Label productionDetails;
	private Label commerceTaxesDetails;
	private Label commerceScienceDetails;
	private Label commerceHappinessDetails;

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

		background.AddChild(new Label() {
			Text = "CULTURE",
			OffsetLeft = 714,
			OffsetTop = 4
		});


		// Load the font we'll use for the details.
		//
		// We skip the cache so that we can change the size without affecting other
		// code using the same font.
		yieldDetailsFont = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf", null, ResourceLoader.CacheMode.Ignore);
		yieldDetailsFont.FixedSize = 20;

		yieldDetailsFontTheme.DefaultFont = yieldDetailsFont;
		yieldDetailsFontTheme.SetColor("font_color", "Label", Colors.Black);
		yieldDetailsFontTheme.SetFontSize("font_size", "Label", 20);

		foodDetails = new Label() {
			OffsetLeft = 290,
			OffsetTop = 567,
			Theme = yieldDetailsFontTheme,
		};
		background.AddChild(foodDetails);

		productionDetails = new Label() {
			OffsetLeft = 290,
			OffsetTop = 520,
			Theme = yieldDetailsFontTheme,
		};
		background.AddChild(productionDetails);

		commerceTaxesDetails = new Label() {
			OffsetLeft = 290,
			OffsetTop = 620,
			Theme = yieldDetailsFontTheme,
		};
		background.AddChild(commerceTaxesDetails);

		commerceScienceDetails = new Label() {
			OffsetLeft = 290,
			OffsetTop = 653,
			Theme = yieldDetailsFontTheme,
		};
		background.AddChild(commerceScienceDetails);

		commerceHappinessDetails = new Label() {
			OffsetLeft = 290,
			OffsetTop = 685,
			Theme = yieldDetailsFontTheme,
		};
		background.AddChild(commerceHappinessDetails);

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
							HandleReassignment(tile);
							RenderPopHeads(tileAssignmentLayer.city);
							RenderFoodDetails(tileAssignmentLayer.city);
							RenderCommerceDetails(tileAssignmentLayer.city);
							RenderProductionDetails(tileAssignmentLayer.city);
						}
					}
				}
			}
		}
	}

	// Returns a list of specialists that this player can use.
	private List<CitizenType> GetKnownSpecialists(Player player) {
		return citizenTypes.FindAll(x => {
			return !x.IsDefaultCitizen && (x.PrerequisiteTech == null || player.knownTechs.Contains(x.PrerequisiteTech));
		});
	}

	private void HandleReassignment(Tile tile) {
		City city = tileAssignmentLayer.city;

		// We only support clicking on workable tiles or the city center.
		if (!city.GetWorkableTiles().Contains(tile) && tile != city.location) {
			return;
		}

		// We can't assign citizens to other cities.
		if (tile.cityAtTile != null && tile.cityAtTile != city) {
			return;
		}

		// We can't assign citizens to tiles worked by other cities.
		if (tile.personWorkingTile != null && tile.personWorkingTile.city != city) {
			return;
		}

		// If we're already working a tile and click on it, turn the citizen
		// into a specialist.
		if (tile.personWorkingTile != null && tile.personWorkingTile.city == city) {
			CityResident resident = tile.personWorkingTile;
			tile.personWorkingTile = null;
			resident.tileWorked = Tile.NONE;
			resident.citizenType = GetKnownSpecialists(city.owner)[0];
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
		mapView.centerCameraOnTile(city.Value.location.neighbors[TileDirection.SOUTH]);
		tileAssignmentLayer.city = city.Value;
		RenderPopHeads(city.Value);
		RenderCulture(city.Value);
		RenderFoodDetails(city.Value);
		RenderCommerceDetails(city.Value);
		RenderProductionDetails(city.Value);
	}

	private void RenderFoodDetails(City city) {
		foodDetails.Text = $"{city.CurrentFoodYield()} food/turn, {city.FoodGrowthPerTurn()} surplus. {city.foodStored} stored. Growth in {city.TurnsUntilGrowth()} turns.";
	}

	private void RenderCommerceDetails(City city) {
		CommerceBreakdown breakdown = city.CurrentCommerceYield();
		commerceTaxesDetails.Text = $"{breakdown.taxes} gold/turn to taxes (0 corrupt)";
		commerceScienceDetails.Text = $"{breakdown.beakers} gold/turn to science  (0 corrupt)";
		commerceHappinessDetails.Text = $"{breakdown.happiness} gold/turn to happiness (0 corrupt)";
	}

	private void RenderProductionDetails(City city) {
		productionDetails.Text = $"{city.CurrentProductionYield()} shields/turn  (0 corrupt). {city.shieldsStored} of {city.itemBeingProduced.shieldCost} stored. {city.TurnsUntilProductionFinished()} turns left.";
	}

	private void RenderCulture(City city) {
		if (culturePerTurn == null) {
			culturePerTurn = new Label() {
				OffsetLeft = 790,
				OffsetTop = 4
			};
			background.AddChild(culturePerTurn);
		}
		culturePerTurn.Text = "0/turn";  // TODO: fill this in

		if (totalCulture == null) {
			totalCulture = new Label() {
				OffsetLeft = 714,
				OffsetTop = 55
			};
			background.AddChild(totalCulture);
		}
		int nextCultureExpansion = (int)Math.Pow(10, city.GetBorderExpansionLevel());
		totalCulture.Text = $"Total: {city.GetCulture()}/{nextCultureExpansion}";
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

			List<CitizenType> specialistTypes = GetKnownSpecialists(city.owner);
			int index = specialistTypes.FindIndex(x => x.Id == cr.citizenType.Id);
			tb.Pressed += () => {
				cr.citizenType = specialistTypes[(index + 1) % specialistTypes.Count];
				++index;
				RenderPopHeads(city);
			};


			background.AddChild(tb);
			popHeads.Add(tb);
			xPos += 48;
		}
	}
}
