using Godot;

public class OreLayer : IChunkGenLayer
{
    public Block Ore;
    public int RollsPerChunk;
    public float VeinProb;
    public float StartDepth;
    public float MaxProbDepth;
    //0 makes 1 ore, 1 makes vein take the whole chunk
    public float VeinSize;

    private FastNoiseLite noise = new FastNoiseLite();
    private float seed;

    public OreLayer()
    {
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }

    public void InitRandom(float seed)
    {
        this.seed = seed;
    }

    public void GenerateChunk(World world, Chunk chunk)
    {
        for (int i = 0; i < RollsPerChunk; i++)
        {
            float sample = 0.5f*(1+noise.GetNoise(seed*12.12345f*i+seed*(float)chunk.Position.x,i+seed*(float)chunk.Position.y,i+seed*(float)chunk.Position.z)); //0..1
            float threshold = Mathf.Lerp(0,VeinProb, Mathf.Clamp((StartDepth-((BlockCoord)chunk.Position).y)/(StartDepth-MaxProbDepth),0,1));
            if (sample < threshold)
            {
                spawnVein(world,chunk, i+1);
            }
        }
    }

    private void spawnVein(World world, Chunk chunk, int vein)
    {
        //0..ChunkSize-1
        int x = (int)(Chunk.CHUNK_SIZE*0.5f*(1+noise.GetNoise(7.123f*seed*-3*vein+seed*(float)chunk.Position.x,7.123f*seed*-3*vein+seed*(float)chunk.Position.y,7.123f*seed*-3*vein+seed*(float)chunk.Position.z)));
        int y = (int)(Chunk.CHUNK_SIZE*0.5f*(1+noise.GetNoise(2.12398f*seed*(-1-3*vein)+seed*(float)chunk.Position.x,2.12398f*seed*(-1-3)*vein+seed*(float)chunk.Position.y,2.12398f*seed*(-1-3)*vein+seed*(float)chunk.Position.z)));
        int z = (int)(Chunk.CHUNK_SIZE*0.5f*(1+noise.GetNoise(3.7398f*seed*(-2-3*vein)+seed*(float)chunk.Position.x,3.7398f*seed*(-2-3*vein)+seed*(float)chunk.Position.y,3.7398f*seed*(-2-3)*vein+seed*(float)chunk.Position.z)));

        for (int i = 0; i < VeinSize; i++)
        {
            //TODO: improve this
            x = Mathf.Clamp(x,0,Chunk.CHUNK_SIZE-1);
            y = Mathf.Clamp(y,0,Chunk.CHUNK_SIZE-1);
            z = Mathf.Clamp(z,0,Chunk.CHUNK_SIZE-1);
            if (chunk[x,y,z] != null) chunk[x,y,z] = Ore;
            x += 1; y+= 1; z+= 1;
        }
        
    }
}