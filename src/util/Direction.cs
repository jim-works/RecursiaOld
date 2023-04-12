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
}