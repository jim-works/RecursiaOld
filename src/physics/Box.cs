using Godot;
using System.Runtime.CompilerServices;

//Axis-Aligned Bounding Box
public struct Box
{
    public Vector3 Corner;
    public Vector3 Size;

    public Box(Vector3 corner, Vector3 size)
    {
        Corner = corner;
        Size = size;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box FromCenter(Vector3 center, Vector3 size)
    {
        return new Box (center-size/2,size);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box FromBlock(BlockCoord block)
    {
        return new Box((Vector3)block, new Vector3(1,1,1));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Center() => Corner+Size/2;

    public bool Inside(Vector3 point)
    {
        return Corner.x <= point.x && point.x <= Corner.x+Size.x
            && Corner.y <= point.y && point.y <= Corner.y+Size.y
            && Corner.z <= point.z && point.z <= Corner.z+Size.z;
    }

    public bool IntersectsBox(Box other)
    {
        return Corner.x <= other.Corner.x+other.Size.x && Corner.x+Size.x >= other.Corner.x
            && Corner.y <= other.Corner.y+other.Size.y && Corner.y+Size.y >= other.Corner.y
            && Corner.z <= other.Corner.z+other.Size.z && Corner.z+Size.z >= other.Corner.z;
    }

}