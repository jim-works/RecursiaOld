using System.Linq;
using Godot;

public struct Spline
{
    private Vector2[] controlPoints;
    public Spline(Vector2[] controlPoints)
    {
        this.controlPoints = controlPoints.OrderBy(p => p.X).ToArray();
    }

    public float Map(float x)
    {
        for(int i = 0; i < controlPoints.Length; i++)
        {
            if (x < controlPoints[i].X)
            {
                if (i == 0) return controlPoints[i].Y;
                float t = (x-controlPoints[i-1].X)/(controlPoints[i].X-controlPoints[i-1].X);
                return Mathf.Lerp(controlPoints[i-1].Y, controlPoints[i].Y, t);
            }
        }
        return controlPoints[controlPoints.Length-1].Y;
    }
}