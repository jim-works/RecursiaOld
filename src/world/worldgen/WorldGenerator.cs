using Godot;
using System.Linq;
using System.Collections.Generic;

public class WorldGenerator
{
    private OpenSimplexNoise noise = new OpenSimplexNoise();

    public void Generate(World world)
    {
        noise.Octaves = 4;
        noise.Period = 128;
        noise.Persistence = 0.8f;
        int worldSize = 3;
        Int2[] sandPillars;
        HashSet<Int2> sandSet = new HashSet<Int2>();
        while (sandSet.Count < 6) {
            sandSet.Add(new Int2((int)(GD.Randi()%(worldSize*Chunk.CHUNK_SIZE)),(int)(GD.Randi()%(worldSize*Chunk.CHUNK_SIZE))));
        }
        sandPillars = sandSet.ToArray();
        Godot.GD.Print(sandPillars.Distinct().Count());
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < worldSize; z++)
                {
                    GenerateChunk(world, world.GetOrCreateChunk(new Int3(x, y, z)), sandPillars);
                }
            }
        }
    }
    public void GenerateChunk(World world, Chunk chunk, Int2[] sandPillarLocations)
    {
        
        Block stone = BlockTypes.Get("stone");
        Block dirt = BlockTypes.Get("dirt");
        Block grass = BlockTypes.Get("grass");
        Block sand = BlockTypes.Get("sand");
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                Int3 noiseCoords = chunk.LocalToWorld(new Int3(x,0,z));
                
                int height = (int)(16*(noise.GetNoise2d(noiseCoords.x,noiseCoords.z)+1));
                for (int y = 0; y < Chunk.CHUNK_SIZE && y < height; y++)
                {
                    Int3 worldCoords = chunk.LocalToWorld(new Int3(x,y,z));
                    if (worldCoords.y < 10) {
                        chunk[x,y,z] = stone;
                    } else if (worldCoords.y < height) {
                        chunk[x,y,z] = dirt;
                    } else if (worldCoords.y == height) {
                        chunk[x,y,z] = grass;
                    }
                }
                foreach (var loc in sandPillarLocations) {
                    if (loc.x == noiseCoords.x && loc.y == noiseCoords.z) {
                        for (int y = 0; y < Chunk.CHUNK_SIZE; y++) {
                            chunk[x,y,z] = sand;
                        }
                    }
                }
            }
        }
    }
}