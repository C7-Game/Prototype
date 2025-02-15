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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		TextureRect background = new() {
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
							HandleReassignment(tile);
						}
					}
				}
			}
		}
	}

	private void HandleReassignment(Tile tile) {
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
	}
}
