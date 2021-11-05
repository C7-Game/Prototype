namespace C7GameData
{
    using System.Collections.Generic;
    /**
     * The game map, at the top level.
     */
    public class GameMap
    {
        int numTilesWide;
        int numTilesTall;
        bool horizontalWrap;

        public List<Tile> tiles {get;}

        public GameMap()
        {
            this.tiles = new List<Tile>();
        }

        /**
         * Another temporary method.  Puppeteer has a better map in the UI.  This just generates a boring, but functional, map.
         **/
        public static GameMap generateDummyGameMap()
        {
            TerrainType grassland = new TerrainType();
            grassland.name = "Grassland";
            grassland.baseFoodProduction = 2;
            grassland.baseShieldProduction = 1; //with only one terrain type, it needs to be > 0
            grassland.baseCommerceProduction = 1;   //same as above
            grassland.movementCost = 1;

            GameMap dummyMap = new GameMap();
            dummyMap.numTilesTall = 40;
            dummyMap.numTilesWide = 40;

            //Uh, right, isometic.  That means we have to stagger things.
            //Also I forget how to do ranges in C#, oh well.
            for (int x = 0; x < dummyMap.numTilesTall; x++)
            {
                int firstYCoordinate = 0;
                if (x % 2 == 1)
                {
                    firstYCoordinate = 1;
                }
                for (int y = firstYCoordinate; y < dummyMap.numTilesWide; y += 2)
                {
                    Tile newTile = new Tile();
                    newTile.xCoordinate = x;
                    newTile.yCoordinate = y;
                    newTile.terrainType = grassland;
                    dummyMap.tiles.Add(newTile);
                }
            }
            return dummyMap;
        }
        // Inputs: noise field width and height, bool whether noise should smoothly wrap X or Y
        // Actual fake-isometric map will have different shape, but for noise we'll go straight 2d matrix
        // NOTE: Apparently this OpenSimplex implementation doesn't do octaves, including persistance or lacunarity
        //  Might be able to implement them, use https://www.youtube.com/watch?v=MRNFcywkUSA&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3&index=4 as reference
        public static double[,] tempMapGenPrototyping(int width, int height, bool wrapX = true, bool wrapY = false)
        {
            // The public domain OpenSiplex implementation always
            //   seems to be 0 at 0,0, so let's offset from it.
            double originOffset = 10;
            double scale = 0.5;
            OpenSimplexNoise noise = new OpenSimplexNoise();
            double[,] noiseField = new double[width, height];

            for (int x=0; x < width; x++)
            {
                double theta = ((double)x / (double)width) * (System.Math.PI * 2);
                // System.Console.WriteLine(theta.ToString("F8"));
                // double cosTheta = System.Math.Cos(theta);
                // double sinTheta = System.Math.Sin(theta);
                double oX = originOffset + (scale * x);
                double cX = originOffset + (scale * System.Math.Sin(theta));
                double cY = originOffset + (scale * System.Math.Cos(theta));
                System.Console.WriteLine(cX.ToString("F8"));
                System.Console.WriteLine(cY.ToString("F8"));
                System.Console.WriteLine("");
                for (int y=0; y < height; y++)
                {
                    double oY = originOffset + (scale * y);
                    if (!(wrapX || wrapY))
                    {
                        // noiseField[x,y] = noise.Evaluate(originOffset + (scale * x), originOffset + (scale * y));
                        noiseField[x,y] = noise.Evaluate(oX, oY);
                    }
                    else
                    {
                        // double cX = ((oX * cosTheta) - (oY * sinTheta));
                        // double cY = ((oX * sinTheta) + (oY * cosTheta));
                        // double cX = ((x * cosTheta) - (y * sinTheta));
                        // double cY = ((x * sinTheta) + (y * cosTheta));
                        if (wrapX && wrapY)
                        {
                            throw new System.ApplicationException("Wrapping both axes not yet implemented");
                            continue;
                        }
                        if (wrapY)
                        {
                            throw new System.ApplicationException("Wrapping Y axis not yet implemented");
                            // noiseField[x,y] = noise.Evaluate(cX, cY, oY);
                        }
                        if (wrapX)
                        {
                            noiseField[x,y] = noise.Evaluate(cX, cY, oY);
                        }
                    }
                }
            }
            double[,] testWrap = new double[width, height];
            for(int x=0;x<width;x++)
              for(int y=0;y<height;y++)
                testWrap[x,y] = noiseField[((int)(0.9*width + x)%width), ((int)(0.9*height + y)%height)];

            return testWrap;
            return noiseField;
        }
    }
}