using Godot;

//currently based off mincraft caves and cliffs generator
//3 noises are used to generate the heightmap: continentalness (2d), which determines overall height and whether we're inland or in an ocean
//erosion (2d), which is basically a multiplier of terrain height, and weirdness (3d), which gives smaller features like overhangs and caves.
public partial class ShapingLayer : IChunkGenLayer
{
    private Block stone;
    private const float terrainScale = 100;
    private const float contFreq = 0.1f, contFreqMult = 3f, contScaleMult = 0.5f;
    private const int contOctaves = 3;
    private const float erosionFreq = 0.2f, erosionFreqMult = 1.5f, erosionScaleMult = 0.7f;
    private const int erosionOctaves = 3;
    private const float weirdFreq = 0.5f, weirdFreqMult = 2f, weirdScaleMult = 0.8f;
    private const int weirdOctaves = 5;
    private const float weirdCutoff = 0.5f;
    private SplineNoise cont;
    private SplineNoise erosion;
    private SplineNoise weird;

    private float seed;
    private const int noiseSampleInterval = 4;
    
    public ShapingLayer()
    {
        stone = BlockTypes.Get("stone");
        LayeredNoise contNoise = new LayeredNoise();
        addLayer(contNoise, contFreq, contFreqMult, contScaleMult, contOctaves);
        cont = new SplineNoise(contNoise, new Vector2[]{new Vector2(-1,-1), new Vector2(-0.75f, -0.25f), new Vector2 (0.75f, 0.5f), new Vector2(1,1)});
        LayeredNoise erosionNoise = new LayeredNoise();
        addLayer(erosionNoise, erosionFreq, erosionFreqMult, erosionScaleMult, erosionOctaves);
        erosion = new SplineNoise(erosionNoise, new Vector2[]{new Vector2(-1,1), new Vector2(0.75f,1), new Vector2(1,0)});
        LayeredNoise weirdNoise = new LayeredNoise();
        addLayer(weirdNoise, weirdFreq, weirdFreqMult, weirdScaleMult, weirdOctaves);
        weird = new SplineNoise(weirdNoise, new Vector2[]{new Vector2(-1,-1), new Vector2(1,1)});
    }

    private void addLayer(LayeredNoise noise, float baseFreq, float freqMult, float scaleMult, int octaves)
    {
        FastNoiseLite fn = new FastNoiseLite();
        fn.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.AddLayers(fn, octaves, baseFreq*Vector3.One, freqMult, 1, scaleMult);
    }

    public void InitRandom(float seed)
    {
        this.seed = seed;
    }

    private float[,] getHeightSamples(Chunk chunk)
    {
        float[,] heightSamples = new float[Chunk.CHUNK_SIZE / noiseSampleInterval + 1, Chunk.CHUNK_SIZE / noiseSampleInterval + 1]; //adding 1 to get one extra layer of samples right outside the chunk border
        BlockCoord cornerWorldCoords = chunk.LocalToWorld(new BlockCoord(0,0,0));
        for (int x = 0; x < heightSamples.GetLength(0); x++)
        {
            for (int z = 0; z < heightSamples.GetLength(1); z++)
            {
                heightSamples[x, z] = terrainScale*cont.Sample(noiseSampleInterval*x+cornerWorldCoords.X,noiseSampleInterval*z+cornerWorldCoords.Z)*erosion.Sample(noiseSampleInterval*x+cornerWorldCoords.X,noiseSampleInterval*z+cornerWorldCoords.Z);
            }
        }
        return heightSamples;
    }
    private float[,,] getWeirdSamples(Chunk chunk) {
        float[,,] weirdSamples = new float[Chunk.CHUNK_SIZE / noiseSampleInterval + 1, Chunk.CHUNK_SIZE / noiseSampleInterval + 1, Chunk.CHUNK_SIZE/noiseSampleInterval + 1];
        BlockCoord cornerWorldCoords = chunk.LocalToWorld(new BlockCoord(0,0,0));
        for (int x = 0; x < weirdSamples.GetLength(0); x++)
        {
            for (int y = 0; y < weirdSamples.GetLength(1); y++)
            {
                for (int z = 0; z < weirdSamples.GetLength(2); z++)
                {
                    weirdSamples[x, y, z] = weird.Sample(noiseSampleInterval*x+cornerWorldCoords.X, noiseSampleInterval*y+cornerWorldCoords.Y, noiseSampleInterval*z+cornerWorldCoords.Z);
                }
            }
        }
        return weirdSamples;
    }
    public void GenerateChunk(World world, Chunk chunk)
    {
        float[,] heightSamples = getHeightSamples(chunk);
        float[,,] weirdSamples = getWeirdSamples(chunk);
        BlockCoord cornerWorldCoords = chunk.LocalToWorld(new BlockCoord(0,0,0));
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {   
                BlockCoord worldCoords = cornerWorldCoords + new BlockCoord(x,0,z);
                int height = (int)Math.Bilerp(new Vector2((float)(x % noiseSampleInterval) / noiseSampleInterval, (float)(z % noiseSampleInterval) / noiseSampleInterval),
                    heightSamples[x / noiseSampleInterval, z / noiseSampleInterval], heightSamples[x / noiseSampleInterval, z / noiseSampleInterval + 1], heightSamples[x / noiseSampleInterval + 1, z / noiseSampleInterval + 1], heightSamples[x / noiseSampleInterval + 1, z / noiseSampleInterval]);
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    float weirdness = Math.Trilerp(new Vector3((float)(x % noiseSampleInterval) / noiseSampleInterval, (float)(y % noiseSampleInterval) / noiseSampleInterval, (float)(z % noiseSampleInterval) / noiseSampleInterval),
                        weirdSamples[x / noiseSampleInterval, y / noiseSampleInterval, z / noiseSampleInterval], weirdSamples[x / noiseSampleInterval, y / noiseSampleInterval + 1, z / noiseSampleInterval], weirdSamples[x / noiseSampleInterval + 1, y / noiseSampleInterval + 1, z / noiseSampleInterval], weirdSamples[x / noiseSampleInterval + 1, y / noiseSampleInterval, z / noiseSampleInterval],
                        weirdSamples[x / noiseSampleInterval, y / noiseSampleInterval, z / noiseSampleInterval + 1], weirdSamples[x / noiseSampleInterval, y / noiseSampleInterval + 1, z / noiseSampleInterval + 1], weirdSamples[x / noiseSampleInterval + 1, y / noiseSampleInterval + 1, z / noiseSampleInterval + 1], weirdSamples[x / noiseSampleInterval + 1, y / noiseSampleInterval, z / noiseSampleInterval + 1]);
                    int worldY = worldCoords.Y+y;
                    if (worldY < height - 5 && weirdness < weirdCutoff) {
                        chunk[x,y,z] = stone;
                    }
                }
            }
        }
    }
}