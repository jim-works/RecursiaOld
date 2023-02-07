using Godot;

public static class Math
{
    //interpolates pos in the unit square where the corners have the values provided by the arguments
    public static float Bilerp(Vector2 pos, float botLeft, float topLeft, float topRight, float botRight)
    {
        return botLeft * (1 - pos.x) * (1 - pos.y) + botRight * pos.x * (1 - pos.y) + topLeft * (1 - pos.x) * pos.y + topRight * pos.x * pos.y;
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