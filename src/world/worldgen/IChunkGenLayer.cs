//Generation layer designed for generation functions that work on a single-chunk basis
//Stuff that doesn't need to interact with neighboring chunks, like heightmap generation
public interface IChunkGenLayer
{
    void InitRandom(float seed);
    void GenerateChunk(World world, Chunk chunk);
}