using Godot;
using System.Runtime.CompilerServices;

//Axis-Aligned Bounding Box
namespace Recursia;
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
        return Corner.X <= point.X && point.X <= Corner.X+Size.X
            && Corner.Y <= point.Y && point.Y <= Corner.Y+Size.Y
            && Corner.Z <= point.Z && point.Z <= Corner.Z+Size.Z;
    }

    public bool IntersectsBox(Box other)
    {
        return Corner.X <= other.Corner.X+other.Size.X && Corner.X+Size.X >= other.Corner.X
            && Corner.Y <= other.Corner.Y+other.Size.Y && Corner.Y+Size.Y >= other.Corner.Y
            && Corner.Z <= other.Corner.Z+other.Size.Z && Corner.Z+Size.Z >= other.Corner.Z;
    }
}