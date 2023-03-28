using Godot;

public static class Math
{
    //interpolates pos in the unit square where the corners have the values provided by the arguments
    public static float Bilerp(Vector2 pos, float botLeft, float topLeft, float topRight, float botRight)
    {
        return botLeft * (1 - pos.X) * (1 - pos.Y) + botRight * pos.X * (1 - pos.Y) + topLeft * (1 - pos.X) * pos.Y + topRight * pos.X * pos.Y;
    }

    public static float Trilerp(Vector3 pos, float botLeft, float topLeft, float topRight, float botRight, float botLeftBack, float topLeftBack, float topRightBack, float botRightBack)
    {
        return botLeft * (1-pos.X)*(1-pos.Y)*(1-pos.Z) + botRight * pos.X*(1-pos.Y)*(1-pos.Z) + topLeft * (1-pos.X)*pos.Y*(1-pos.Z) + topRight * pos.X*pos.Y*(1-pos.Z) +
            botLeftBack * (1-pos.X)*(1-pos.Y)*pos.Z + botRightBack * pos.X*(1-pos.Y)*pos.Z + topLeftBack * (1-pos.X)*pos.Y*pos.Z + topRightBack * pos.X*pos.Y*pos.Z;
    }

    public static float Trilerp(float[,,] samples, int x, int y, int z, int sampleInterval)
    {
        return Math.Trilerp(new Vector3((float)(x % sampleInterval) / sampleInterval, (float)(y % sampleInterval) / sampleInterval, (float)(z % sampleInterval) / sampleInterval),
                        samples[x / sampleInterval, y / sampleInterval, z / sampleInterval], samples[x / sampleInterval, y / sampleInterval + 1, z / sampleInterval], samples[x / sampleInterval + 1, y / sampleInterval + 1, z / sampleInterval], samples[x / sampleInterval + 1, y / sampleInterval, z / sampleInterval],
                        samples[x / sampleInterval, y / sampleInterval, z / sampleInterval + 1], samples[x / sampleInterval, y / sampleInterval + 1, z / sampleInterval + 1], samples[x / sampleInterval + 1, y / sampleInterval + 1, z / sampleInterval + 1], samples[x / sampleInterval + 1, y / sampleInterval, z / sampleInterval + 1]);
    }
    
    //returns the max magnitude component of the vector with the other two components zeroed
    //if multple components are equal, the first one is set the others are zeroed
    public static Vector3 MaxComponent(Vector3 source)
    {
        if (Mathf.Abs(source.X)>Mathf.Abs(source.Y)&&Mathf.Abs(source.X)>Mathf.Abs(source.Z))
        {
            return new Vector3(source.X,0,0);
        }
        if (Mathf.Abs(source.Y)>Mathf.Abs(source.Z))
        {
            return new Vector3(0,source.Y,0);
        }
        return new Vector3(0,0,source.Z);
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