public class HeightmapLayer : IChunkGenLayer
{
    private Block stone, dirt, grass, copper;
    private const float noiseFreq = 0.25f;
    private const float noiseScale = 25;
    private FastNoiseLite noise = new FastNoiseLite();

    public HeightmapLayer()
    {
        stone = BlockTypes.Get("stone");
        dirt = BlockTypes.Get("dirt");
        grass = BlockTypes.Get("grass");
        copper = BlockTypes.Get("copper_ore");
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }

    public void GenerateChunk(World world, Chunk chunk)
    {
        BlockCoord cornerWorldCoords = chunk.LocalToWorld(new BlockCoord(0,0,0));
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {   
                BlockCoord worldCoords = cornerWorldCoords + new BlockCoord(x,0,z);
                int height = (int)(noiseScale*noise.GetNoise(worldCoords.x*noiseFreq,worldCoords.z*noiseFreq));
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    int worldY = worldCoords.y+y;
                    if (worldY < height - 5) {
                        if (x%6-z%6-y%6<-4) chunk[x,y,z] = copper;
                        else chunk[x,y,z] = stone;
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