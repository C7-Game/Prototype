using Godot;
using System;
using C7GameData;

public partial class CityUI : Control
{
	private City city;
	private Label cityName;

	public override void _Ready() {
		cityName = GetNode<Label>("TopUI/CityName");
	}

	public void SetCity(City city) {
		this.city = city;
		cityName.Text = city.name;
	}

}
