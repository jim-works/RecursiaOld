using Godot;

public partial class DetailLayer : IChunkGenLayer
{
    private Block stone;
    private Block grass;
    private Block dirt;
    private const int dirtDepth = 1;

    public DetailLayer()
    {
        stone = BlockTypes.Get("stone");
        grass = BlockTypes.Get("grass");
        dirt = BlockTypes.Get("dirt");
    }

    public void InitRandom(System.Func<int> seeds)
    {
    }


    public void GenerateChunk(World world, Chunk chunk)
    {
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (chunk[x,y,z] == grass) {
                        for (int c = y; c > 0 && c > y-dirtDepth; c--) {
                            if (chunk[x,c,z] == stone) {
                                chunk[x,c,z] = dirt;
                            }
                        }
                    } 
                }
            }
        }
    }
}