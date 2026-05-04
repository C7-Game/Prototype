using System.Collections.Generic;
using Godot;
using C7GameData;
using C7Engine;
using System;

[Tool]
[GlobalClass]
public partial class ProductionMenu : Civ3TextureRect {
	private Dictionary<TreeItem, IProducible> itemMapping = new();

	Tree tree;
	Theme fontTheme = new();

	public ProductionMenu() { }

	public override void _Ready() {
		this.Texture = TextureLoader.Load("city_screen.production_queue");

		// Load the font we'll use.
		FontFile font = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf", null, ResourceLoader.CacheMode.Ignore);
		font.FixedSize = 10;
		fontTheme.DefaultFont = font;
	}

	public void AddItems(GameData gameData, City city, Action<IProducible> chooseProduction) {
		if (tree != null) {
			itemMapping.Clear();
			RemoveChild(tree);
			tree = null;
		}
		tree = new();

		// Set up the tree of items. We use a tree so we get a scroll bar and
		// other niceties.
		AddChild(tree);
		tree.Columns = 2;
		tree.Size = new Vector2(203, 360);
		tree.SetColumnExpand(0, true);
		tree.SetColumnExpand(1, false);
		tree.SetColumnCustomMinimumWidth(1, 50);
		TradingTree.ConfigureTreeTheme(tree, fontTheme);

		TreeItem root = TradingTree.CreateTreeRoot(tree);

		foreach (IProducible option in city.ListProductionOptions(gameData)) {
			int buildTime = city.TurnsToProduce(option);

			TreeItem child = tree.CreateItem(root);
			string text = $"{option.name}";
			if (option is UnitPrototype proto) {
				string attackDesc = (proto.bombard > 0) ? $"{proto.attack}({proto.bombard})" : proto.attack.ToString();
				text += $" {attackDesc}.{proto.defense}.{proto.movement}";
			}
			child.SetText(0, text);
			child.SetText(1, $"{buildTime} turns");
			child.SetIcon(0, RightClickChooseProductionMenu.GetProducibleIcon(option, city.owner));
			child.SetCustomMinimumHeight(40);
			child.SetAutowrapMode(0, TextServer.AutowrapMode.WordSmart);
			tree.SetColumnTitleAlignment(1, HorizontalAlignment.Right);
			itemMapping[child] = option;
		}

		// We only want clickable behavior, not selectable behavior.
		tree.ItemSelected += () => {
			TreeItem ti = tree.GetSelected();
			ti.Deselect(0);
			chooseProduction(itemMapping[ti]);
		};
	}
}
