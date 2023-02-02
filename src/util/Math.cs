using Godot;

public static class Math
{
    //bilinear interpolation over [0,1]x[0,1] at sample point (dx,dy).
    //xAyB is the corner at (A,B), so x0y0 is the origin (0,0)
    public static float Bilerp(float dx, float dy, float x0y0, float x1y0, float x0y1, float x1y1) {
        return x0y0 * (1 - dx) * (1 - dy) + x1y0 * dx * (1 - dy) + x0y1 * (1 - dx) * dy + x1y1 * dx * dy;
    }
    
    //returns the max magnitude component of the vector with the other two components zeroed
    //if multple components are equal, the first one is set the others are zeroed
    public static Vector3 MaxComponent(Vector3 source)
    {
        if (Mathf.Abs(source.x)>Mathf.Abs(source.y)&&Mathf.Abs(source.x)>Mathf.Abs(source.z))
        {
            return new Vector3(source.x,0,0);
        }
        if (Mathf.Abs(source.y)>Mathf.Abs(source.z))
        {
            return new Vector3(0,source.y,0);
        }
        return new Vector3(0,0,source.z);
    }

    //fast exponent - only for positive pow
    public static int Pow(int x, int pow)
    {
        int ret = 1;
        while (pow != 0)
        {
            if ((pow & 1) == 1)
                ret *= x;
            x *= x;
            pow >>= 1;
        }
        return ret;
    }
}