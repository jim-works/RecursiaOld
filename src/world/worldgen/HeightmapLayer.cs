using Godot;

public partial class HeightmapLayer : IChunkGenLayer
{
    private Block stone, dirt, grass, copper;
    private const float noiseFreq = 0.2f;
    private const float noiseScale = 50;
    private const float celluarFreq = 0.2f;
    private const float cellularScale = 1f;
    private LayeredNoise noise = new LayeredNoise();

    private float seed;
    private const int noiseSampleInterval = 4;
    
    public HeightmapLayer()
    {
        stone = BlockTypes.Get("stone");
        dirt = BlockTypes.Get("dirt");
        grass = BlockTypes.Get("grass");
        FastNoiseLite fn = new FastNoiseLite();
        fn.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.AddLayers(fn, 5, noiseFreq*Vector3.One, 2f, noiseScale, 0.7f);
        FastNoiseLite cellular = new FastNoiseLite();
        cellular.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noise.AddProductLayer(cellular, celluarFreq*Vector3.One, cellularScale);
    }

    public void InitRandom(float seed)
    {
        this.seed = seed;
    }

    public void GenerateChunk(World world, Chunk chunk)
    {
        float[,] heightSamples = new float[Chunk.CHUNK_SIZE / noiseSampleInterval + 1, Chunk.CHUNK_SIZE / noiseSampleInterval + 1]; //adding 1 to get one extra layer of samples right outside the chunk border
        BlockCoord cornerWorldCoords = chunk.LocalToWorld(new BlockCoord(0,0,0));
        for (int x = 0; x < heightSamples.GetLength(0); x++)
        {
            for (int z = 0; z < heightSamples.GetLength(1); z++)
            {
                heightSamples[x, z] = noise.Sample(noiseSampleInterval*x+cornerWorldCoords.X,noiseSampleInterval*z+cornerWorldCoords.Z);
            }
        }
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