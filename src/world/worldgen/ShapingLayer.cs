using Godot;

public partial class ShapingLayer : IChunkGenLayer
{
    private Block stone;
    private Block grass;
    private const int noiseSampleInterval = 4;
    private const float freq = 0.1f, freqMult = 3f, scaleMult = 0.5f;
    private const int octaves = 5;
    private SplineNoise densityNoise;
    private Spline densityByHeight = new Spline(new Vector2[]{new Vector2(-50,-0.5f), new Vector2(0,0), new Vector2(25,0.5f), new Vector2(100,1)});

    //reduces density for (flattens) above ground terrain. low frequency
    private const float scaleFreq = 0.2f, scaleFreqMult = 1.5f, scaleScaleMult = 0.5f;
    private const int scaleOctaves = 3;
    private SplineNoise scaleNoise;

    //noise that can create wacky effects by multiplying the density by a high value rarely
    private const float wackyFreq = 0.7f, wackyFreqMult = 1.5f, wackyScaleMult = 0.7f;
    private const int wackyOctaves = 3;
    private SplineNoise wackyNoise;

    public ShapingLayer()
    {
        stone = BlockTypes.Get("stone");
        grass = BlockTypes.Get("grass");
        DebugInfoLabel.Inputs.Add((pos) => $"Density: {densityNoise.Sample(pos.X, pos.Y, pos.Z).ToString("0.00")}\n Scale: {scaleNoise.Sample(pos.X, pos.Z).ToString("0.00")}\n Wacky: {wackyNoise.Sample(pos.X, pos.Y, pos.Z).ToString("0.00")}");
    }

    public void InitRandom(System.Func<int> seeds)
    {
        LayeredNoise layeredNoise = new LayeredNoise(seeds());
        layeredNoise.AddSumLayers(freq, freqMult, scaleMult, octaves);
        densityNoise = new SplineNoise(layeredNoise, new Spline(new Vector2[]{new Vector2(-1,-1), new Vector2(1,1)}));

        LayeredNoise scaleLayers = new LayeredNoise(seeds());
        scaleLayers.AddSumLayers(scaleFreq, scaleFreqMult, scaleScaleMult, scaleOctaves);
        scaleNoise = new SplineNoise(scaleLayers, new Spline(new Vector2[]{new Vector2(-1,2), new Vector2(scaleLayers.Quantile(0.1f), 1), new Vector2(scaleLayers.Quantile(0.5f), 0.8f), new Vector2(scaleLayers.Quantile(0.8f),0.5f), new Vector2(1,0)}));

        LayeredNoise wackyLayers = new LayeredNoise(seeds());
        wackyLayers.AddSumLayers(wackyFreq, wackyFreqMult, wackyScaleMult, wackyOctaves);
        wackyNoise = new SplineNoise(wackyLayers, new Spline(new Vector2[]{new Vector2(-1,1), new Vector2(wackyLayers.Quantile(0.9f), 1), new Vector2(1f, 3)}));
    }

    private float sample(float x, float y, float z) {
        float density = densityNoise.Sample(x,y,z);
        //scale scales the terrain. straight multiplication pushes or pulls values from 0
        float scale = scaleNoise.Sample(x,z);
        density*=scale;
        //wackyNoise can create wacky effects by multiplying the density by a high value rarely
        density*=wackyNoise.Sample(x,y,z);
        return density;
    }

    private float[,,] getSamples(ChunkCoord chunk) {
        //extra y value to sample a couple blocks above the chunk for grass and dirt
        float[,,] samples = new float[Chunk.CHUNK_SIZE / noiseSampleInterval + 1, Chunk.CHUNK_SIZE / noiseSampleInterval + 2, Chunk.CHUNK_SIZE/noiseSampleInterval + 1];
        BlockCoord cornerWorldCoords = (BlockCoord)chunk;
        for (int x = 0; x < samples.GetLength(0); x++)
        {
            for (int y = 0; y < samples.GetLength(1); y++)
            {
                for (int z = 0; z < samples.GetLength(2); z++)
                {
                    samples[x, y, z] = sample(noiseSampleInterval*x+cornerWorldCoords.X, noiseSampleInterval*y+cornerWorldCoords.Y, noiseSampleInterval*z+cornerWorldCoords.Z);
                }
            }
        }
        return samples;
    }

    public void GenerateChunk(World world, Chunk chunk)
    {
        float[,,] samples = getSamples(chunk.Position);
        BlockCoord cornerWorldCoords = chunk.LocalToWorld(new BlockCoord(0,0,0));
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    chunk[x, y, z] = GetBlockType(samples, cornerWorldCoords, x, y, z);
                }
            }
        }
    }

    public Block GetBlockType(float[,,] samples, BlockCoord worldCoords, int x, int y, int z)
    {
        if (densityByHeight.Map(worldCoords.Y+y) < Math.Trilerp(samples, x, y, z,noiseSampleInterval))
        {
            return densityByHeight.Map(worldCoords.Y+y+1) < Math.Trilerp(samples, x, y+1, z,noiseSampleInterval) ? stone : grass;
        }
        return null;
    }
}