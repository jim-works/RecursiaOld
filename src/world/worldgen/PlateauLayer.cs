using Godot;

public partial class PlateauLayer : IChunkGenLayer
{
    private Block stone, dirt, grass;
    private const float noiseFreq = 0.2f;
    private const float noiseScale = 50;
    private const float celluarFreq = 0.2f;
    private const float cellularScale = 1f;
    private LayeredNoise plateauNoise = new LayeredNoise();
    private LayeredNoise layerNoise = new LayeredNoise();
    private const float tierMult = 0.025f;
    private const float layerMult = 5;
    private const float layerFreq = 0.5f;

    private float seed;
    private const int noiseSampleInterval = 4;
    
    public PlateauLayer()
    {
        stone = BlockTypes.Get("stone");
        dirt = BlockTypes.Get("dirt");
        grass = BlockTypes.Get("grass");

        //setup main shape of plateaus
        FastNoiseLite fn = new FastNoiseLite();
        fn.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        plateauNoise.AddLayers(fn, 5, noiseFreq*Vector3.One, 2f, noiseScale, 0.7f);
        FastNoiseLite cellular = new FastNoiseLite();
        cellular.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        plateauNoise.AddProductLayer(cellular, celluarFreq*Vector3.One, cellularScale);

        //setup detail noise of plateau layers
        FastNoiseLite layerFn = new FastNoiseLite((int)seed);
        layerFn.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        layerNoise.AddLayers(layerFn, 2, Vector3.One*layerFreq, 5, layerMult, 0.4f);
    }

    public void InitRandom(float seed)
    {
        this.seed = seed;
    }

    private float[,] getSample(ChunkCoord coord)
    {
        float[,] heightSamples = new float[Chunk.CHUNK_SIZE / noiseSampleInterval + 1, Chunk.CHUNK_SIZE / noiseSampleInterval + 1]; //adding 1 to get one extra layer of samples right outside the chunk border
        BlockCoord cornerWorldCoords = (BlockCoord)coord;
        for (int x = 0; x < heightSamples.GetLength(0); x++)
        {
            for (int z = 0; z < heightSamples.GetLength(1); z++)
            {
                float sample = plateauNoise.Sample(noiseSampleInterval * x + cornerWorldCoords.X, noiseSampleInterval * z + cornerWorldCoords.Z);;
                heightSamples[x, z] = (1/tierMult)*(Mathf.Floor(sample*tierMult))+layerMult*layerNoise.Sample(noiseSampleInterval * x + cornerWorldCoords.X, noiseSampleInterval * z + cornerWorldCoords.Z);
            }
        }
        return heightSamples;
    }

    public void GenerateChunk(World world, Chunk chunk)
    {
        float[,] heightSamples = getSample(chunk.Position);
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
                    int worldY = worldCoords.Y+y;
                    if (worldY < height - 5) {
                        chunk[x,y,z] = stone;
                    } else if (worldY < height) {
                        chunk[x,y,z] = dirt;
                    } else if (worldY == height) {
                        chunk[x,y,z] = grass;
                    }
                }
            }
        }
    }
}