
namespace Recursia;
public static class Divide
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static int ToNegative(int lhs, int rhs)
    {
        return (lhs / rhs) + ((lhs % rhs) >> 31);
    }
}
