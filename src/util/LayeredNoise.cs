using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using Godot;

public class LayeredNoise
{
    private struct Layer
    {
        public FastNoiseLite noise;
        public Vector3 freq;
        public float amp;
    }
    private List<Layer> layers = new List<Layer>();
    private float sumNoiseMagnitude = 0;
    public int Seed {get; private set;}
    private float variance;

    public LayeredNoise(int seed = 1337)
    {
        Seed = seed;
    }

    public float Scale => sumNoiseMagnitude;
    public void AddLayer(FastNoiseLite layer, Vector3 freq, float amp)
    {
        layers.Add(new Layer{
            noise=layer,
            freq=freq,
            amp=amp
        });
        sumNoiseMagnitude += amp;
        //all layers are centered on 0, so we can just add the variance of each layer
        //uniform variance is width^2/12 = (2*amp)^2/12 = amp^2/3
        variance += amp*amp/3;
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

    private float Sample(float x, float y)
    {
        float res = 0;
        foreach (var layer in layers)
        {
            res += layer.amp*layer.noise.GetNoise(layer.freq.X*x,layer.freq.Y*y);
        }
        return res;
    }

    //rescales so that max is 1 and min is -1
    public float SampleNorm(float x, float y) => Sample(x,y)/Scale;

    private float Sample(float x, float y, float z)
    {
        float res = 0;
        foreach (var layer in layers)
        {
            res += layer.amp*layer.noise.GetNoise(layer.freq.X*x,layer.freq.Y*y,layer.freq.Z*z);
        }
        return res;
    }

    //rescales so that max is 1 and min is -1
    public float SampleNorm(float x, float y, float z) => Sample(x,y,z)/Scale;

    //treats each layer as a uniform distribution from [-amp,amp], scales so that max sum is 1/min sum is -1
    //uses rough normal approxiamtion (CLT)
    public float Cdf(double t)
    {
        return (float)Normal.CDF(0,Mathf.Sqrt(variance)/Scale,t);
    }
    //inverse cdf
    //treats each layer as a uniform distribution from [-amp,amp], scales so that max sum is 1/min sum is -1
    //uses rough normal approxiamtion (CLT)
    public float Quantile(double q)
    {
        float res = (float)Normal.InvCDF(0,Mathf.Sqrt(variance)/Scale,q);
        return res;
    }
}