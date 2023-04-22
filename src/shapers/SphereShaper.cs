using System.Collections.Generic;
using Godot;

namespace Recursia;
public static class SphereShaper
{
    public static void Shape3D(World world, Vector3 origin, float strength)
    {
        BlockCoord minBounds = new((int)(origin.X-strength),(int)(origin.Y-strength),(int)(origin.Z-strength));
        BlockCoord maxBounds = new((int)(origin.X + strength), (int)(origin.Y + strength), (int)(origin.Z + strength));
        BlockCoord originInt = (BlockCoord)origin;
        world.Chunks.BatchSetBlock((setter) =>
        {
            for (int x = minBounds.X; x < maxBounds.X; x++)
            {
                for (int y = minBounds.Y; y < maxBounds.Y; y++)
                {
                    for (int z = minBounds.Z; z < maxBounds.Z; z++)
                    {
                        BlockCoord p = new(x, y, z);
                        float sqrDist = (p - originInt).sqrMag();
                        if (sqrDist > strength * strength) continue; //outside of blast radius 
                        setter(p, null);
                    }
                }
            }
        });
    }
}