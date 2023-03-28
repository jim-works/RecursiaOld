using System.Collections.Generic;
using Godot;

public class LayeredNoise
{
    private struct Layer
    {
        public FastNoiseLite noise;
        public Vector3 freq;
        public float amp;
    }
    private List<Layer> sumLayer = new List<Layer>();
    private List<Layer> prodLayer = new List<Layer>();
    private float sumNoiseMagnitude = 0;
    private float prodNoiseMagnitude = 1;
    public int Seed {get; private set;}

    public LayeredNoise(int seed = 1337)
    {
        Seed = seed;
    }

    public float Scale => sumNoiseMagnitude*prodNoiseMagnitude;
    public void AddLayer(FastNoiseLite layer, Vector3 freq, float amp)
    {
        sumLayer.Add(new Layer{
            noise=layer,
            freq=freq,
            amp=amp
        });
        sumNoiseMagnitude += amp;
    }
    public void AddSumLayers(float baseFreq, float freqMult, float scaleMult, int octaves)
    {
        FastNoiseLite fn = new FastNoiseLite(Seed);
        fn.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        AddLayers(fn, octaves, baseFreq*Vector3.One, freqMult, 1, scaleMult);
    }

    public void AddLayers(FastNoiseLite layer, int n, Vector3 baseFreq, float freqMult, float baseAmp, float ampMult)
    {
        float currFreqMult = 1;
        float currAmpMult = 1;
        for (int i = 0; i < n; i++)
        {
            AddLayer(layer, currFreqMult*baseFreq, currAmpMult*baseAmp);
            currAmpMult *= ampMult;
            currFreqMult *= freqMult;
        }
    }

    public void AddProductLayer(FastNoiseLite layer, Vector3 freq, float amp)
    {
        prodLayer.Add(new Layer{
            noise=layer,
            freq=freq,
            amp=amp
        });
        prodNoiseMagnitude *= amp;
    }

    public float Sample(float x, float y)
    {
        float res = 0;
        foreach (var layer in sumLayer)
        {
            res += layer.amp*layer.noise.GetNoise(layer.freq.X*x,layer.freq.Y*y);
        }
        foreach (var layer in prodLayer)
        {
            res *= layer.amp*layer.noise.GetNoise(layer.freq.X*x,layer.freq.Y*y);
        }
        return res;
    }

    public float SampleNorm(float x, float y) => Sample(x,y)/Scale;

    public float Sample(float x, float y, float z)
    {
        float res = 0;
        foreach (var layer in sumLayer)
        {
            res += layer.amp*layer.noise.GetNoise(layer.freq.X*x,layer.freq.Y*y,layer.freq.Z*z);
        }
        foreach (var layer in prodLayer)
        {
            res *= layer.amp*layer.noise.GetNoise(layer.freq.X*x,layer.freq.Y*y,layer.freq.Z*z);
        }
        return res;
    }

    public float SampleNorm(float x, float y, float z) => Sample(x,y,z)/Scale;
}