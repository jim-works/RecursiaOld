using System.Threading.Tasks;
using Godot;

namespace Recursia;
public class SkyTreeStructureProvider : WorldStructureProvider
{
    private const int LEAF_SIZE = 50;
    private const float LEAF_START_PROPORTION = 0.9f; //what proportion up the trunk to start leaf generation.
    private const float BRANCH_START_PROPORTION = 0.25f; //do not generate branches if they are too close to base
    private const int TRUNK_HEIGHT = 100;
    //trunk is (thickness+1)x(thickness+1)
    private const int BASE_THICKNESS = 6;
    private const int TOP_THICKNESS = 3;
    private const float CUTOFF = LEAF_SIZE * LEAF_SIZE / 2;
    private const float FREQ = 0.1f;
    private readonly float ODDS = 0.10f;
    private const float BASE_NOISE = 1f;
    private const int MAX_BRANCHES = 3;
    private const int MAX_BRANCH_DEPTH = 3;
    private readonly Block? log;
    private readonly Block? grass;
    private readonly Block? leaves;
    private readonly FastNoiseLite leafNoise = new();

    public SkyTreeStructureProvider(uint seed) : base(seed, new BlockCoord(LEAF_SIZE * 2 + 1, TRUNK_HEIGHT + LEAF_SIZE, LEAF_SIZE * 2 + 1),
        new WorldStructure("SkyTree")
        {
            Mutex = false,
            Priority = 0
        })
    {
        RollsPerChunk = 1;
        BlockTypes.TryGet("grass", out grass);
        BlockTypes.TryGet("log", out log);
        BlockTypes.TryGet("leaves", out leaves);

        leafNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        leafNoise.SetFrequency(FREQ);
    }
    public override bool SuitableLocation(Chunk c, BlockCoord coord)
    {
        return c[coord] == grass; //tree must be planted on grass
    }
    public override WorldStructure? PlaceStructure(ChunkCollection chunks, BlockCoord position)
    {
        //noise is -1..1, remap to 0..1
        //GD.Print($"sky tree at {position} noise : {(1 + spawnNoise.GetNoise(position.X, position.Y, position.Z)) / 2} threshold: {ODDS}");
        if (GD.Randf() > ODDS)
        {
            return null;
        }
        chunks.BatchSetBlock(set => genBranch(set, Direction.PosY, position, 0, 1,1,1));
        return null;
    }
    private void genBranch(System.Action<BlockCoord, Block?> set, Direction dir, BlockCoord start, int depth, float trunkLengthMult, float trunkThicknessMult, float leafMult)
    {
        if (depth > MAX_BRANCH_DEPTH) return;
        int length = (int)(TRUNK_HEIGHT*trunkLengthMult);
        int maxThickness= (int)(BASE_THICKNESS*trunkThicknessMult);
        int minThickness= depth > 1 ? maxThickness : (int)(TOP_THICKNESS*trunkThicknessMult);
        int leafSize= (int)(LEAF_SIZE*leafMult);
        BlockCoord delta = dir.ToBlockCoord();
        BlockCoord basisX = dir.GetPerpendicular(0).ToBlockCoord();
        BlockCoord basisY = dir.GetPerpendicular(1).ToBlockCoord();
        int branchCount = 0;
        int branchStarti = (int)(length*BRANCH_START_PROPORTION);
        int leafHeight = (int)(length * LEAF_START_PROPORTION);
        for (int x = -leafSize; x <= leafSize; x++)
        {
            for (int y = -leafSize; y <= leafSize; y++)
            {
                for (int z = -leafSize; z <= leafSize; z++)
                {
                    //remap sample from -1..1 to 0..1
                    float sample = (x * x + y * y + z * z) * (BASE_NOISE/trunkLengthMult + 0.5f * (1 + leafNoise.GetNoise(start.X + x, start.Y + y, start.Z + z)));
                    if (sample < CUTOFF*leafMult) set(new BlockCoord(x, y, z)+leafHeight*delta + start, leaves);
                }
            }
        }
        for (int i = 0; i < length; i++)
        {
            int thickness = Mathf.RoundToInt(Mathf.Lerp(maxThickness, minThickness, (float)i / length));
            if (i>= branchStarti && i<=leafHeight-leafSize && i%(length/MAX_BRANCHES) == 0 && branchCount < MAX_BRANCHES)
            {
                branchCount++;
                //"randomly" generate a branch perpendicular to the trunk
                BlockCoord middle = start+i*delta;
                Direction branchDir = dir.GetPerpendicular(Rng.Sample(Seed,middle));
                genBranch(set, branchDir, middle+thickness*branchDir.ToBlockCoord(), depth+1, trunkLengthMult*0.4f,trunkThicknessMult*0.5f,leafMult*0.35f);
            }
            for (int x = -thickness; x <= thickness; x++)
            {
                for (int y = -thickness; y <= thickness; y++)
                {
                    set(start + i * delta + x * basisX + y * basisY, log);
                }
            }
        }
    }
}