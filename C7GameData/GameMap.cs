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
            int octaves = 3;
            double persistence = 0.5;
            // The public domain OpenSiplex implementation always
            //   seems to be 0 at 0,0, so let's offset from it.
            double originOffset = 0;
            double scale = 0.06;
            double xRadius = (double)width / (System.Math.PI * 2);
            double yRadius = (double)height / (System.Math.PI * 2);
            OpenSimplexNoise noise = new OpenSimplexNoise();
            double[,] noiseField = new double[width, height];

            for (int x=0; x < width; x++)
            {
                double theta = ((double)x / (double)width) * (System.Math.PI * 2);
                double oX = originOffset + (scale * x);
                double cX = originOffset + (scale * xRadius * System.Math.Sin(theta));
                double cY = originOffset + (scale * xRadius * System.Math.Cos(theta));
                for (int y=0; y < height; y++)
                {
                    double oY = originOffset + (scale * y);
                    double yTheta = ((double)y / (double)height) * (System.Math.PI * 2);
                    double ycX = originOffset + (scale * yRadius * System.Math.Sin(yTheta));
                    double ycY = originOffset + (scale * yRadius * System.Math.Cos(yTheta));
                    if (!(wrapX || wrapY))
                    {
                        noiseField[x,y] = noise.Evaluate(oX, oY);
                    }
                    else
                    {
                        if (wrapX && wrapY)
                        {
                            throw new System.ApplicationException("Wrapping both axes not yet implemented");
                            continue;
                        }
                        if (wrapY)
                        {
                            for (int i=0;i<octaves;i++)
                            {
                                double offset = i * 1.5 * System.Math.Max(width, height) * scale;
                                double a = ycX + offset;
                                double b = ycY + offset;
                                double c = oX + offset;
                                noiseField[x,y] += (octaves - i) * persistence * noise.Evaluate(a, b, c);
                            }
                        }
                        if (wrapX)
                        {
                            for (int i=0;i<octaves;i++)
                            {
                                double offset = i * 1.5 * System.Math.Max(width, height) * scale;
                                double a = cX + offset;
                                double b = cY + offset;
                                double c = oY + offset;
                                noiseField[x,y] += (octaves - i) * persistence * noise.Evaluate(a, b, c);
                            }
                        }
                    }
                }
            }
            // temporarily shifting noiseField to make it easier to visually spot good/bad wrapping
            double[,] testWrap = new double[width, height];
            for(int x=0;x<width;x++)
              for(int y=0;y<height;y++)
                testWrap[x,y] = noiseField[((int)(0.9*width + x)%width), ((int)(0.9*height + y)%height)];

            return testWrap;
            return noiseField;
        }
    }
}