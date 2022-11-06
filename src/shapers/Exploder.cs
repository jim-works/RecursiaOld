using System.Collections.Generic;
using Godot;

public static class Exploder
{
    public static void Explode(World world, Vector3 origin, float strength)
    {
        //TODO: make this better
        //expanding cube at the center of the explosion. keep track of cumulative power using a 3d array
        //r^4 algorithm -> r^3
        BlockCoord minBounds = new BlockCoord((int)(origin.x-strength),(int)(origin.y-strength),(int)(origin.z-strength));
        BlockCoord maxBounds = new BlockCoord((int)(origin.x + strength), (int)(origin.y + strength), (int)(origin.z + strength));
        BlockCoord originInt = (BlockCoord)origin;
        List<BlockcastHit> buffer = new List<BlockcastHit>();
        for (int x = minBounds.x; x < maxBounds.x; x++)
        {
            for (int y = minBounds.y; y < maxBounds.y; y++)
            {
                for (int z = minBounds.z; z < maxBounds.z; z++)
                {
                    BlockCoord p = new BlockCoord(x,y,z);
                    float sqrDist = (p-originInt).sqrMag();
                    if (sqrDist > strength*strength) continue; //outside of blast radius 
                    float power = strength-Mathf.Sqrt(sqrDist);
                    world.BlockcastAll((Vector3)originInt, (Vector3)p, buffer);
                    foreach (var item in buffer)
                    {
                        if (item.Block != null) power -= item.Block.ExplosionResistance;
                    }
                    buffer.Clear();
                    if (power > 0) world.SetBlock(p, null);
                }
            }
        }
    }
}