using System.Collections.Generic;
using Godot;

public static class SphereShaper
{
    public static void Shape3D(World world, Vector3 origin, float strength)
    {
        BlockCoord minBounds = new BlockCoord((int)(origin.X-strength),(int)(origin.Y-strength),(int)(origin.Z-strength));
        BlockCoord maxBounds = new BlockCoord((int)(origin.X + strength), (int)(origin.Y + strength), (int)(origin.Z + strength));
        BlockCoord originInt = (BlockCoord)origin;
        for (int x = minBounds.X; x < maxBounds.X; x++)
        {
            for (int y = minBounds.Y; y < maxBounds.Y; y++)
            {
                for (int z = minBounds.Z; z < maxBounds.Z; z++)
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