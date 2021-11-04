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
        MapNode.Position = new Vector2(-32, -16);
        AddChild(MapNode);
        GenerateMap();
    }
    protected void GenerateMap()
    {
        GD.Print("generating map");
        int mapWidth = 32;
        int mapHeight = 24;
        OpenSimplexNoise simplex = new OpenSimplexNoise();
        int[,] noiseMap = new int[mapWidth, mapHeight];
        for (int x=0; x<mapWidth; x++)
        {
            for (int y=0; y<mapHeight; y++)
            {
                float noise = simplex.GetNoise2d(x, y);
                int value;
                if (noise < (float)0) value = 13;
                else value = 0;
                noiseMap[x,y] = value;
            }

        }

        MapNode.MapWidth = mapWidth;
        MapNode.MapHeight = mapHeight;
        MapTiles = new List<GenTile>();
		for (int y = 0; y < mapHeight; y++) {
			for (int x = (y % 2); x < mapWidth; x+=2) {
				int Top = y == 0 ? (noiseMap[(x+1) % mapWidth,y]) : (noiseMap[x,y-1]);
				int Bottom = y == mapHeight - 1 ? (noiseMap[(x+1) % mapWidth,y]) : (noiseMap[x,y+1]);
                /*
				string foo = 
					(noiseMap[(x+1) % mapWidth,y]).ToString("D3") +
					Bottom.ToString("D3") +
					(noiseMap[Mathf.Abs((x-1) % mapWidth),y]).ToString("D3") +
					Top.ToString("D3")
				;
				try {
				// noiseMap[x,y] = (int)Terrmask["001001001001"];
				noiseMap[x,y] = (int)Terrmask[foo];
				} catch { GD.Print(x + "," + y + " " + foo); }
                */
                GenTile newTile = new GenTile();
                newTile.LegacyX = x;
                newTile.LegacyY = y;
                newTile.LegacyFileID = 1;
                newTile.LegacyImageID = 1;
                MapTiles.Add(newTile);
			}
		}
        /*
        for (int y=0; y<mapHeight; y++)
        {
            for (int x=(y%2); x<mapWidth; x+=2)
            {
                GenTile newTile = new GenTile();
                newTile.LegacyX = x;
                newTile.LegacyY = y;
                newTile.LegacyFileID = 1;
                newTile.LegacyImageID = new Random().Next(0,81);

                MapTiles.Add(newTile);
            }
        }
        */
        MapNode.LegacyTiles = MapTiles;
        MapNode.TerrainAsTileMap();
    }
    public void _on_Regenerate_pressed()
    {
        GenerateMap();
    }
}
