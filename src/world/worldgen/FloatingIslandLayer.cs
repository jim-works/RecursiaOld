using Godot;

public partial class FloatingIslandLayer : IChunkGenLayer
{
    private Block sand;
    private const float cutoffFreq = 0.1f;
    private const float cutoff = 1f;
    private const int heightOffset = 50;
    private const float heightFreq = 0.5f;
    private const float heightScale = 50f;
    private LayeredNoise cutoffNoise = new LayeredNoise();
    private LayeredNoise heightNoise = new LayeredNoise();

    private float seed;
    private const int noiseSampleInterval = 4;
    
    public FloatingIslandLayer()
    {
        sand = BlockTypes.Get("sand");


    }

    public void InitRandom(float seed)
    {
        this.seed = seed;
        //maxNoise-minNoise > some value generates a block for the island
        //setup noise for bottom of islands
        FastNoiseLite minFn = new FastNoiseLite((int)seed);
        minFn.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        cutoffNoise.AddLayers(minFn, 2, cutoffFreq * Vector3.One, 2f, 1, 0.7f);

        //setup noise for top of islands
        FastNoiseLite maxFn = new FastNoiseLite((int)seed);
        maxFn.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        heightNoise.AddLayers(maxFn, 2, heightFreq * Vector3.One, 2f, heightScale, 0.7f);
    }

    private float[,,] getSample(ChunkCoord coord)
    {
        float[,,] heightSamples = new float[Chunk.CHUNK_SIZE / noiseSampleInterval + 1, Chunk.CHUNK_SIZE / noiseSampleInterval + 1,2]; //adding 1 to get one extra layer of samples right outside the chunk border
        BlockCoord cornerWorldCoords = (BlockCoord)coord;
        for (int x = 0; x < heightSamples.GetLength(0); x++)
        {
            for (int z = 0; z < heightSamples.GetLength(1); z++)
            {
                heightSamples[x,z,0] = cutoffNoise.Sample(noiseSampleInterval * x + cornerWorldCoords.X, noiseSampleInterval * z + cornerWorldCoords.Z);
                heightSamples[x,z,1] = heightNoise.Sample(noiseSampleInterval * x + cornerWorldCoords.X, noiseSampleInterval * z + cornerWorldCoords.Z);
            }
        }
        return heightSamples;
    }

    public void GenerateChunk(World world, Chunk chunk)
    {
        float[,,] heightSamples = getSample(chunk.Position);
        BlockCoord cornerWorldCoords = chunk.LocalToWorld(new BlockCoord(0,0,0));
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {   
                BlockCoord worldCoords = cornerWorldCoords + new BlockCoord(x,0,z);
                float cutoffVal = Math.Bilerp(new Vector2((float)(x % noiseSampleInterval) / noiseSampleInterval, (float)(z % noiseSampleInterval) / noiseSampleInterval),
                    heightSamples[x / noiseSampleInterval, z / noiseSampleInterval,0], heightSamples[x / noiseSampleInterval, z / noiseSampleInterval + 1,0], heightSamples[x / noiseSampleInterval + 1, z / noiseSampleInterval + 1,0], heightSamples[x / noiseSampleInterval + 1, z / noiseSampleInterval,0]);
                int islandHeight = (int)Math.Bilerp(new Vector2((float)(x % noiseSampleInterval) / noiseSampleInterval, (float)(z % noiseSampleInterval) / noiseSampleInterval),
                    heightSamples[x / noiseSampleInterval, z / noiseSampleInterval, 1], heightSamples[x / noiseSampleInterval, z / noiseSampleInterval + 1, 1], heightSamples[x / noiseSampleInterval + 1, z / noiseSampleInterval + 1, 1], heightSamples[x / noiseSampleInterval + 1, z / noiseSampleInterval, 1]);
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    int worldY = worldCoords.Y+y;
                    int height = Mathf.Abs(islandHeight)+heightOffset;
                    if (cutoffVal < cutoff) continue;
                    if (worldY < height && worldY > heightOffset*(1+cutoffVal)) {
                        chunk[x,y,z] = sand;
                    }
                }
            }
        }
    }
}