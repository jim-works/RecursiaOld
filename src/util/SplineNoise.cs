using Godot;
using System.Linq;

public class SplineNoise
{
    private LayeredNoise noise;
    private Spline spline;
    public SplineNoise(LayeredNoise noise, Spline spline)
    {
        this.noise = noise;
        this.spline = spline;
    }

    public float Sample(float x, float y) => Sample(x,y,0);

    public float Sample(float x, float y, float z) => spline.Map(noise.SampleNorm(x,y,z));
}