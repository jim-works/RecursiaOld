using System.Collections.Generic;
using Godot;

public static class SphereShaper
{
    public static void Shape(World world, Vector3 origin, float strength)
    {
        BlockCoord minBounds = new BlockCoord((int)(origin.x-strength),(int)(origin.y-strength),(int)(origin.z-strength));
        BlockCoord maxBounds = new BlockCoord((int)(origin.x + strength), (int)(origin.y + strength), (int)(origin.z + strength));
        BlockCoord originInt = (BlockCoord)origin;
        for (int x = minBounds.x; x < maxBounds.x; x++)
        {
            for (int y = minBounds.y; y < maxBounds.y; y++)
            {
                for (int z = minBounds.z; z < maxBounds.z; z++)
                {
                    BlockCoord p = new BlockCoord(x,y,z);
                    float sqrDist = (p-originInt).sqrMag();
                    if (sqrDist > strength*strength) continue; //outside of blast radius 
                    world.SetBlock(p, null);
                }
            }
        }
    }
}