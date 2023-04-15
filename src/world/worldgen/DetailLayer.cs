using Godot;

namespace Recursia;
public class DetailLayer : IChunkGenLayer
{
    private readonly Block? stone;
    private readonly Block? grass;
    private readonly Block? dirt;
    private const int dirtDepth = 2;

    public DetailLayer()
    {
        BlockTypes.TryGet("stone", out stone);
        BlockTypes.TryGet("grass", out grass);
        BlockTypes.TryGet("dirt", out dirt);
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
                        for (int c = y; c > 0 && c >= y-dirtDepth; c--) {
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