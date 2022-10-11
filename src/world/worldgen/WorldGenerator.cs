public class WorldGenerator
{
    public void Generate(World world)
    {
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    GenerateChunk(world, world.GetOrCreateChunk(new Int3(x, y, z)));
                }
            }
        }
    }
    public void GenerateChunk(World world, Chunk chunk)
    {
        Block stone = BlockTypes.Get("stone");
        Block dirt = BlockTypes.Get("dirt");
        Block grass = BlockTypes.Get("grass");
        Block sand = BlockTypes.Get("sand");
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    Int3 worldCoords = chunk.LocalToWorld(new Int3(x,y,z));
                    if (worldCoords.y < 10) {
                        chunk[x,y,z] = stone;
                    } else if (worldCoords.y < 20) {
                        chunk[x,y,z] = dirt;
                    } else if (worldCoords.y == 20) {
                        chunk[x,y,z] = grass;
                    }
                    if (worldCoords.x == 0 && worldCoords.z == 0) {
                        chunk[x,y,z] = sand;
                    }
                }
            }
        }
    }
}