using Godot;
using System;

namespace Recursia;
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
        BlockCoord max = new(Mathf.FloorToInt(min.X), Mathf.CeilToInt(corner.Y+ySize), Mathf.CeilToInt(corner.Z+zSize));
        for (int y = min.Y; y < max.Y; y++)
        {
            for (int z = min.Z; z < max.Z; z++)
            {
                BlockCoord curr = new(max.X,y,z);
                Block? test = world.Chunks.GetBlock(curr);
                if (test?.Collidable == true) return true;
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
        BlockCoord max = new(Mathf.CeilToInt(corner.X+xSize), Mathf.FloorToInt(min.Y), Mathf.CeilToInt(corner.Z+zSize));
        for (int x = min.X; x < max.X; x++)
        {
            for (int z = min.Z; z < max.Z; z++)
            {
                BlockCoord curr = new(x,max.Y,z);
                Block? test = world.Chunks.GetBlock(curr);
                if (test?.Collidable == true) return true;
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
        BlockCoord max = new(Mathf.CeilToInt(corner.X+xSize), Mathf.CeilToInt(corner.Y+ySize), Mathf.FloorToInt(min.Z));
        for (int x = min.X; x < max.X; x++)
        {
            for (int y = min.Y; y < max.Y; y++)
            {
                BlockCoord curr = new(x,y,max.Z);
                Block? test = world.Chunks.GetBlock(curr);
                if (test?.Collidable == true) return true;
            }
        }
        return false;
    }
}