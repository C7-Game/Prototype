using Godot;
using System;
using System.Collections.Generic;

public class MapGenTest : Node2D
{
    private class GenTile : LegacyMap.ILegacyTile
	{
		public int LegacyFileID { get; set;}
		public int LegacyImageID { get; set;}
		public int LegacyX {get; set;}
		public int LegacyY {get; set;}
	}
    List<GenTile> MapTiles;
    protected LegacyMap MapNode;
    public override void _Ready()
    {
        MapNode = new LegacyMap();
        AddChild(MapNode);
        GenerateMap();
    }
    protected void GenerateMap()
    {
        GD.Print("generating map");
        int mapWidth = 4;
        int mapHeight = 4;
        MapNode.MapWidth = mapWidth;
        MapNode.MapHeight = mapHeight;
        MapTiles = new List<GenTile>();
        for (int x=0; x<mapWidth; x++)
        {
            for (int y=0; y<mapHeight; y++)
            {
                GenTile newTile = new GenTile();
                newTile.LegacyX = x;
                newTile.LegacyY = y;
                newTile.LegacyFileID = 1;
                newTile.LegacyImageID = new Random().Next(0,81);

                MapTiles.Add(newTile);
            }
        }
        MapNode.LegacyTiles = MapTiles;
        MapNode.TerrainAsTileMap();
    }
    public void _on_Regenerate_pressed()
    {
        GenerateMap();
    }
}
