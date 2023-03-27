using Godot;
using System.Linq;

public class SplineNoise
{
    private LayeredNoise noise;
    private Vector2[] controlPoints;
    public SplineNoise(LayeredNoise noise, Vector2[] controlPoints)
    {
        this.noise = noise;
        this.controlPoints = controlPoints.OrderBy(p => p.X).ToArray();
    }

    public float Sample(float x, float y) => Sample(x,y,0);

    //multiplies the noise by a spline function (lerp between control points)
    public float Sample(float x, float y, float z)
    {
        float baseSample = noise.SampleNorm(x,y,z);
        for(int i = 0; i < controlPoints.Length; i++)
        {
            if (baseSample < controlPoints[i].X)
            {
                if (i == 0) return controlPoints[i].Y;
                float t = (baseSample-controlPoints[i-1].X)/(controlPoints[i].X-controlPoints[i-1].X);
                return Mathf.Lerp(controlPoints[i-1].Y, controlPoints[i].Y, t);
            }
        }
        return controlPoints[controlPoints.Length-1].Y;
    }
}