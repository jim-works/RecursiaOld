using Godot;
using System.Linq;

namespace Recursia;
public class SplineNoise
{
    private readonly LayeredNoise noise;
    private readonly Spline spline;
    public SplineNoise(LayeredNoise noise, Spline spline)
    {
        this.noise = noise;
        this.spline = spline;
    }

    public float Sample(float x, float y) => Sample(x,y,0);

    public float Sample(float x, float y, float z) => spline.Map(noise.SampleNorm(x,y,z));
}