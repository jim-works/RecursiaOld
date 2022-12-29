using Godot;

public static class Math
{
    //bilinear interpolation over [0,1]x[0,1] at sample point (dx,dy).
    //xAyB is the corner at (A,B), so x0y0 is the origin (0,0)
    public static float Bilerp(float dx, float dy, float x0y0, float x1y0, float x0y1, float x1y1) {
        return x0y0 * (1 - dx) * (1 - dy) + x1y0 * dx * (1 - dy) + x0y1 * (1 - dx) * dy + x1y1 * dx * dy;
    }
    
    //returns the max component of the vector with the other two components zeroed
    //if multple components are equal, the first one is set the others are zeroed
    public static Vector3 MaxComponent(Vector3 source)
    {
        if (source.x>source.y&&source.x>source.z)
        {
            return new Vector3(source.x,0,0);
        }
        if (source.y>source.z)
        {
            return new Vector3(0,source.y,0);
        }
        return new Vector3(0,0,source.z);
    }
}