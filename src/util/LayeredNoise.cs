using System.Collections.Generic;
using Godot;

public partial class LayeredNoise
{
    private struct Layer
    {
        public FastNoiseLite noise;
        public Vector3 freq;
        public float amp;
    }
    private List<Layer> sumLayer = new List<Layer>();
    private List<Layer> prodLayer = new List<Layer>();
    public float Seed;
    public void AddLayer(FastNoiseLite layer, Vector3 freq, float amp)
    {
        sumLayer.Add(new Layer{
            noise=layer,
            freq=freq,
            amp=amp
        });
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
    }

    public float Sample(float x, float y)
    {
        float res = 0;
        foreach (var layer in sumLayer)
        {
            res += layer.amp*layer.noise.GetNoise(Seed+layer.freq.X*x,Seed+layer.freq.Y*y,Seed);
        }
        foreach (var layer in prodLayer)
        {
            res *= layer.amp*layer.noise.GetNoise(Seed+layer.freq.X*x,Seed+layer.freq.Y*y,Seed);
        }
        return res;
    }

    public float Sample(float x, float y, float z)
    {
        float res = 0;
        foreach (var layer in sumLayer)
        {
            res += layer.amp*layer.noise.GetNoise(Seed+layer.freq.X*x,Seed+layer.freq.Y*y,Seed+layer.freq.Z*z);
        }
        foreach (var layer in prodLayer)
        {
            res *= layer.amp*layer.noise.GetNoise(Seed+layer.freq.X*x,Seed+layer.freq.Y*y,Seed+layer.freq.Z*z);
        }
        return res;
    }
}