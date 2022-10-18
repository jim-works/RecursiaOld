using Godot;
using System.Linq;
using System.Collections.Generic;

public class WorldGenerator
{
    private OpenSimplexNoise noise = new OpenSimplexNoise();
    private const float noiseScale = 100;
    private const int sampleRatio = 4;
    private const int samples = Chunk.CHUNK_SIZE/sampleRatio;

    public void Generate(World world)
    {
        noise.Octaves = 4;
        noise.Period = 128;
        noise.Persistence = 0.8f;
        int worldSize = 15;
        var noiseCache = new float[samples+1,samples+1,samples+1];
        for (int x = -worldSize; x < 0; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                for (int z = -worldSize; z < 0; z++)
                {
                    GenerateChunk(world, world.GetOrCreateChunk(new BlockCoord(x, y, z)), noiseCache);
                }
            }
        }
        world.SetBlock(new BlockCoord(-1,0,-1), null);
    }
    public void GenerateChunk(World world, Chunk chunk, float[,,] noiseCache)
    {
        for (int x = 0; x < samples+1; x++) {
            for (int z = 0; z < samples+1; z++) {
                BlockCoord noiseCoords = chunk.LocalToWorld(new BlockCoord(x*sampleRatio,0,z*sampleRatio));
                noiseCache[x,0,z] = noiseScale*(noise.GetNoise2d(noiseCoords.x,noiseCoords.z)+1);
            }
        }
        Block stone = BlockTypes.Get("stone");
        Block dirt = BlockTypes.Get("dirt");
        Block grass = BlockTypes.Get("grass");
        Block sand = BlockTypes.Get("sand");
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {   
                Int2 sp = new Int2(x/samples,z/samples);
                Vector2 propInSample = new Vector2((x - sp.x*samples)/(float)samples,(z- sp.y*samples)/(float)samples);
                int height = (int)Math.Bilerp(propInSample.x,propInSample.y,noiseCache[sp.x,0,sp.y],noiseCache[sp.x+1,0,sp.y],noiseCache[sp.x,0,sp.y+1],noiseCache[sp.x+1,0,sp.y+1]);
                for (int y = 0; y < Chunk.CHUNK_SIZE && y < height; y++)
                {
                    BlockCoord worldCoords = chunk.LocalToWorld(new BlockCoord(x,y,z));
                    if (worldCoords.y < height - 5) {
                        chunk[x,y,z] = stone;
                    } else if (worldCoords.y < height) {
                        chunk[x,y,z] = dirt;
                    } else if (worldCoords.y == height) {
                        chunk[x,y,z] = grass;
                    }
                }
            }
        }
    }
}