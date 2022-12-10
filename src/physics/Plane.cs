using Godot;
using System;

public static class Plane
{
    //Plane normal = (1,0,0)
    //corners are (corner) and (corner + (0,ySize,zSize))
    //corner should be the more negative corner
    //Faces of blocks are on integral coordinates
    //Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
    public static bool CollidesWithWorldX(Vector3 corner, float ySize, float zSize, World world)
    {
        BlockCoord min = (BlockCoord)corner;
        BlockCoord max = new BlockCoord(min.x, Mathf.CeilToInt(corner.y+ySize), Mathf.CeilToInt(corner.z+zSize));
        for (int y = min.y; y <= max.y; y++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                BlockCoord curr = new BlockCoord(min.x,y,z);
                Block test = world.GetBlock(curr);
                if (test != null && test.Collidable) return true;
            }
        }
        return false;
    }

    //Plane normal = (0,1,0)
    //corners are (corner) and (corner + (xSize,0,zSize))
    //corner should be the more negative corner
    //Faces of blocks are on integral coordinates
    //Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
    public static bool CollidesWithWorldY(Vector3 corner, float xSize, float zSize, World world)
    {
        BlockCoord min = (BlockCoord)corner;
        BlockCoord max = new BlockCoord(Mathf.CeilToInt(corner.x+xSize), min.y, Mathf.CeilToInt(corner.z+zSize));
        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                BlockCoord curr = new BlockCoord(x,min.y,z);
                Block test = world.GetBlock(curr);
                if (test != null && test.Collidable) return true;
            }
        }
        return false;
    }

    //Plane normal = (0,0,1)
    //corners are (corner) and (corner + (0,ySize,zSize))
    //corner should be the more negative corner
    //Faces of blocks are on integral coordinates
    //Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
    public static bool CollidesWithWorldZ(Vector3 corner, float xSize, float ySize, World world)
    {
        BlockCoord min = (BlockCoord)corner;
        BlockCoord max = new BlockCoord(Mathf.CeilToInt(corner.x+xSize), Mathf.CeilToInt(corner.y+ySize), min.z);
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                BlockCoord curr = new BlockCoord(x,y,min.z);
                Block test = world.GetBlock(curr);
                if (test != null && test.Collidable) return true;
            }
        }
        return false;
    }
}