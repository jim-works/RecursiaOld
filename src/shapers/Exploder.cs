using System.Collections.Generic;
using Godot;

public static class Exploder
{
    public static void Explode(World world, Vector3 origin, float strength)
    {
        //TODO: make this better
        //expanding cube at the center of the explosion. keep track of cumulative power using a 3d array
        //r^4 algorithm -> r^3
        BlockCoord minBounds = new BlockCoord((int)(origin.X-strength),(int)(origin.Y-strength),(int)(origin.Z-strength));
        BlockCoord maxBounds = new BlockCoord((int)(origin.X + strength), (int)(origin.Y + strength), (int)(origin.Z + strength));
        BlockCoord originInt = (BlockCoord)origin;
        List<BlockcastHit> buffer = new List<BlockcastHit>();
        for (int x = minBounds.X; x < maxBounds.X; x++)
        {
            for (int y = minBounds.Y; y < maxBounds.Y; y++)
            {
                for (int z = minBounds.Z; z < maxBounds.Z; z++)
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