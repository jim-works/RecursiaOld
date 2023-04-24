namespace Recursia;

public static class Rng
{
    private const uint iterations = 16;
    private static uint rot(uint x)
    {
        return (x << 16) | (x >> 16);
    }
    public static uint Sample(uint seed, BlockCoord coord) => Sample(seed,coord.X,coord.Y, coord.Z);
    public static uint Sample(uint seed, ChunkCoord coord) => Sample(seed,coord.X,coord.Y, coord.Z);
    public static uint Sample(uint seed, int x, int y, int z)
    {
        for (uint i = 0; i < iterations; i++)
        {
            seed = seed * 541 + (uint)x;
            seed = rot(seed);
            seed = seed * 809 + (uint)y;
            seed = rot(seed);
            seed = seed * 1009 + (uint)z;
            seed = rot(seed);
            seed = seed * 673 + i;
            seed = rot(seed);
        }
        return seed;
    }
    public static bool CoinFlip(uint numerator, uint denom, uint seed, BlockCoord coord) => CoinFlip(numerator, denom,seed,coord.X,coord.Y, coord.Z);
    public static bool CoinFlip(uint numerator, uint denom, uint seed, ChunkCoord coord) => CoinFlip(numerator, denom,seed,coord.X,coord.Y, coord.Z);
    public static bool CoinFlip(uint numerator, uint denom, uint seed, int x, int y, int z)
    {
        return (Sample(seed,x,y,z)%denom)<numerator;
    }
}