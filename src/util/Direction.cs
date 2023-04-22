using System.Runtime.CompilerServices;
namespace Recursia;

public enum Direction
{
    //doing this so we can test direction axis by bit ops: ((4|x)^NegX)==0 tests if its on x-axis
    PosX,
    PosY,
    PosZ,
    NegX,
    NegY,
    NegZ,
}

public static class DirectionUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MaskHas(int mask, Direction d) => ((mask>>(int)d) & 1) ==1;
    //gets each of the 4 perpendicular directions depending on the value of i (0..3)
    //if i is outside this range, it gets wrapped back in.
    public static Direction GetPerpendicular(this Direction d, int i)
    {
        i = System.Math.Abs(i)%4;
        int x = (1+i+(int)d)%6;
        return i >= 2 ? (Direction)((x+1)%6) : (Direction)x;
    }
    //get corresponding vector for direction
    public static Godot.Vector3 ToVector3(this Direction d)
    {
        return d switch
        {
            Direction.PosX => Godot.Vector3.Right,
            Direction.PosY => Godot.Vector3.Up,
            Direction.PosZ => Godot.Vector3.Back,
            Direction.NegX => Godot.Vector3.Left,
            Direction.NegY => Godot.Vector3.Down,
            Direction.NegZ => Godot.Vector3.Forward,
            _ => throw new System.ArgumentOutOfRangeException(nameof(d)),
        };
    }
        //get corresponding vector for direction
    public static BlockCoord ToBlockCoord(this Direction d)
    {
        return d switch
        {
            Direction.PosX => new BlockCoord(1,0,0),
            Direction.PosY => new BlockCoord(0,1,0),
            Direction.PosZ => new BlockCoord(0,0,1),
            Direction.NegX => new BlockCoord(-1,0,0),
            Direction.NegY => new BlockCoord(0,-1,0),
            Direction.NegZ => new BlockCoord(0,0,-1),
            _ => throw new System.ArgumentOutOfRangeException(nameof(d), d, "should be 0-5"),
        };
    }
}