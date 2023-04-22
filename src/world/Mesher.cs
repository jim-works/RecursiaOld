using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
namespace Recursia;
public partial class Mesher : Node
{
    [Export]
    public Material? ChunkMaterial;
    [Export]
    public float MaxMeshTime = 0.1f;
    [Export]
    public int MeshIntervalMs = 1000;
    private float meshTimer;
    public static Mesher? Singleton {get; private set;}
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> toMesh = new();
    private readonly Dictionary<ChunkCoord, int> meshing = new();
    private readonly Dictionary<ChunkCoord, ChunkMesh> done = new();
    private readonly ConcurrentDictionary<ChunkCoord, Chunk> waitingToMesh = new();
    private readonly ConcurrentBag<(ChunkMesh, ChunkCoord)> finishedMeshes = new();
    private World world = null!;
    //private Pool<ChunkMesh> meshPool = new Pool<ChunkMesh>(() => new ChunkMesh(), m => m.Node != null, m => m.ClearData(), 100);
    // Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
        Singleton = this;
        world = GetParent<World>();
        GD.Print("Mesher initialized!");
        base._EnterTree();
    }
    public override void _Ready()
    {
        world.Chunks.OnChunkUnload += Unload;
        world.Chunks.OnChunkUpdate += OnChunkUpdate;
        world.Chunks.OnChunkLoad += MeshDeferred;
        GD.Print("Mesher initialized!");
        base._Ready();
    }
    public override void _Process(double delta)
    {
        //multithread chunk generation
        foreach(var kvp in toMesh.ToArray())
        {
            if (toMesh.TryRemove(kvp.Key, out Chunk? c))
                multithreadGenerateChunk(c);
        }
        //spawn all on single thread to avoid a million race conditions
        while (!finishedMeshes.IsEmpty)
        {
            if (finishedMeshes.TryTake(out (ChunkMesh, ChunkCoord) pair))
            {
                //keep track of number of times this chunk is being meshed atm
                meshing[pair.Item2]--;
                if (meshing[pair.Item2] == 0) meshing.Remove(pair.Item2);
                //only spawn chunks if they're not being meshed again, and only spawn the most up to date mesh
                if (!meshing.ContainsKey(pair.Item2) && (!done.TryGetValue(pair.Item2, out ChunkMesh? other) || other.Timestamp <= pair.Item1.Timestamp))
                {
                    done[pair.Item2] = pair.Item1;
                }
            }
        }
        foreach (var kvp in done)
        {
            spawnChunk(kvp.Value, kvp.Key);
        }
        done.Clear();
        base._Process(delta);
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.IsPressed() && key.Keycode == Key.C)
        {
            ChunkCoord cpos = (ChunkCoord)Player.LocalPlayer!.Position;
            if (world.Chunks.TryGetChunk(cpos, out Chunk? c))
            {
                string info = $"Meshed: {c.GetMeshedHistory()}, waiting to be meshed: {waitingToMesh.ContainsKey(cpos)}, state: {c.State}, generation state: {c.GenerationState}\nevent hist: {c.GetEventHistory()}";
                GD.Print($"Chunk at {cpos}: {info}");
            }
        }
        base._Input(@event);
    }
    public void Unload(Chunk chunk, ChunkBuffer? _)
    {
        if (chunk == null) return;
        chunk.Mesh?.ClearData();
        waitingToMesh.TryRemove(chunk.Position, out Chunk _);
        chunk.Mesh = null;
        chunk.Meshed = false;
    }
    public void MeshDeferred(Chunk c)
    {
        c.Meshed = false;
        if (canMesh(c))
        {
            toMesh[c.Position] = c;
            waitingToMesh.TryRemove(c.Position, out Chunk _);
        }
        else
        {
            waitingToMesh[c.Position] = c;
        }

        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(1,0,0), out Chunk? c1) && canMesh(c1)) {
            MeshDeferred(c1);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(-1,0,0), out Chunk? c2) && canMesh(c2)) {
            MeshDeferred(c2);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,1,0), out Chunk? c3) && canMesh(c3)) {
            MeshDeferred(c3);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,-1,0), out Chunk? c4) && canMesh(c4)) {
            MeshDeferred(c4);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,0,1), out Chunk? c5) && canMesh(c5)) {
            MeshDeferred(c5);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,0,-1), out Chunk? c6) && canMesh(c6)) {
            MeshDeferred(c6);
        }
    }
    public void OnChunkUpdate(Chunk c)
    {
        MeshDeferred(c);
        if (world.Chunks.TryGetChunk(c.Position + new ChunkCoord(1,0,0), out Chunk? c1)) MeshDeferred(c1);
        if (world.Chunks.TryGetChunk(c.Position + new ChunkCoord(-1,0,0), out Chunk? c2)) MeshDeferred(c2);
        if (world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,1,0), out Chunk? c3)) MeshDeferred(c3);
        if (world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,-1,0), out Chunk? c4)) MeshDeferred(c4);
        if (world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,0,1), out Chunk? c5)) MeshDeferred(c5);
        if (world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,0,-1), out Chunk? c6)) MeshDeferred(c6);
    }
    public bool canMesh(Chunk c)
    {
        //only mesh if all adjacent chunks are generated
        return c.GenerationState >= ChunkGenerationState.GENERATED && c.State >= ChunkState.Loaded
        && world.Chunks.TryGetChunk(c.Position + new ChunkCoord(1,0,0), out Chunk? c1) && c1.GenerationState == ChunkGenerationState.GENERATED
        && world.Chunks.TryGetChunk(c.Position + new ChunkCoord(-1,0,0), out Chunk? c2) && c2.GenerationState == ChunkGenerationState.GENERATED
        && world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,1,0), out Chunk? c3) && c3.GenerationState == ChunkGenerationState.GENERATED
        && world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,-1,0), out Chunk? c4) && c4.GenerationState == ChunkGenerationState.GENERATED
        && world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,0,1), out Chunk? c5) && c5.GenerationState == ChunkGenerationState.GENERATED
        && world.Chunks.TryGetChunk(c.Position + new ChunkCoord(0,0,-1), out Chunk? c6) && c6.GenerationState == ChunkGenerationState.GENERATED;
    }
    //applies mesh to chunk, removes old mesh if needed, spawns chunk in scene as a child as this node
    private void spawnChunk(ChunkMesh mesh, ChunkCoord coord)
    {
        if (!world.Chunks.TryGetChunk(coord, out Chunk? chunk)) return;
        chunk.Meshed = true;
        chunk.Mesh?.ClearData();
        chunk.Mesh = mesh;
        chunk.AddEvent("spawned");
        if (mesh.Verts.Count == 0)
        {
            //no need to spawn in a new MeshInstance3D if the chunk is empty
            return;
        }
        MeshInstance3D meshNode = new();
        chunk.Mesh.ApplyTo(meshNode, ChunkMaterial);
        AddChild(chunk.Mesh.Node);
    }
    //places finished chunk in finishedMeshes
    private void multithreadGenerateChunk(Chunk chunk) {
        if (!meshing.TryAdd(chunk.Position, 1)) meshing[chunk.Position]++;

        Chunk c = chunk;
        Task.Run(() => generateAndQueueChunk(c));
    }
    private void generateAndQueueChunk(Chunk c) {
        ChunkMesh mesh = generateMesh(c);
        finishedMeshes.Add((mesh, c.Position));
    }
    private static ChunkMesh getMesh()
    {
        return new ChunkMesh();
    }
    private ChunkMesh generateMesh(Chunk chunk)
    {
        chunk.AddEvent("mesh generated");
        ChunkMesh chunkMesh = getMesh();
        chunkMesh.Timestamp = Godot.Time.GetTicksUsec();
        var vertices = chunkMesh.Verts;
        var tris = chunkMesh.Tris;
        var normals = chunkMesh.Norms;
        var uvs = chunkMesh.UVs;

        Chunk?[] neighbors = new Chunk?[6]; //we are in 3d
        neighbors[(int)Direction.PosX] = world.Chunks.GetChunkOrNull(chunk.Position + new ChunkCoord(1,0,0));
        neighbors[(int)Direction.PosY] = world.Chunks.GetChunkOrNull(chunk.Position + new ChunkCoord(0,1,0));
        neighbors[(int)Direction.PosZ] = world.Chunks.GetChunkOrNull(chunk.Position + new ChunkCoord(0,0,1));
        neighbors[(int)Direction.NegX] = world.Chunks.GetChunkOrNull(chunk.Position + new ChunkCoord(-1,0,0));
        neighbors[(int)Direction.NegY] = world.Chunks.GetChunkOrNull(chunk.Position + new ChunkCoord(0,-1,0));
        neighbors[(int)Direction.NegZ] = world.Chunks.GetChunkOrNull(chunk.Position + new ChunkCoord(0,0,-1));

        //generate the mesh
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    meshBlock(chunk, neighbors, new BlockCoord(x,y,z), chunk[x,y,z], vertices, uvs, normals, tris);
                }
            }
        }

        if (vertices.Count==0) {
            chunkMesh.ClearData();
        }
        return chunkMesh;
    }
    private static void meshBlock(Chunk chunk, Chunk?[] neighbors, BlockCoord localPos, Block? block, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        static bool shouldAddFace(Chunk? c, bool transparent, int x, int y, int z) => c == null || c[x,y,z] == null || !transparent && c[x,y,z]!.Transparent;
        if (block == null) return;
        AtlasTextureInfo tex = block.TextureInfo;
        bool transparent = block.Transparent;
        Vector3 pos = (Vector3)chunk.LocalToWorld(localPos);

        //addFace if block is opaque and other is transparent or null
        //addFace if block is transparent and other is null

        //check if there's no block/a transparent block in each direction. only generate face if so.
        if (localPos.X == 0 && shouldAddFace(neighbors[(int)Direction.NegX], transparent, (int)Chunk.CHUNK_SIZE-1,localPos.Y,localPos.Z) || localPos.X != 0 && shouldAddFace(chunk, transparent,localPos.X-1,localPos.Y,localPos.Z)) {
            addFacePosX(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Y == 0 && shouldAddFace(neighbors[(int)Direction.NegY], transparent, localPos.X, (int)Chunk.CHUNK_SIZE-1,localPos.Z) || localPos.Y != 0 && shouldAddFace(chunk, transparent,localPos.X,localPos.Y-1,localPos.Z)) {
            addFaceNegY(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Z == 0 && shouldAddFace(neighbors[(int)Direction.NegZ], transparent, localPos.X,localPos.Y,(int)Chunk.CHUNK_SIZE-1) || localPos.Z != 0 && shouldAddFace(chunk, transparent,localPos.X,localPos.Y,localPos.Z-1)) {
            addFacePosZ(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.X == Chunk.CHUNK_SIZE-1 && shouldAddFace(neighbors[(int)Direction.PosX], transparent, 0,localPos.Y,localPos.Z) || localPos.X != Chunk.CHUNK_SIZE-1 && shouldAddFace(chunk, transparent,localPos.X+1,localPos.Y,localPos.Z)) {
            addFaceNegX(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Y == Chunk.CHUNK_SIZE-1 && shouldAddFace(neighbors[(int)Direction.PosY], transparent, localPos.X,0,localPos.Z) || localPos.Y != Chunk.CHUNK_SIZE-1 && shouldAddFace(chunk, transparent,localPos.X,localPos.Y+1,localPos.Z)) {
            addFacePosY(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Z == Chunk.CHUNK_SIZE-1 && shouldAddFace(neighbors[(int)Direction.PosZ], transparent, localPos.X,localPos.Y,0) || localPos.Z != Chunk.CHUNK_SIZE-1 && shouldAddFace(chunk, transparent,localPos.X,localPos.Y,localPos.Z+1)) {
            addFaceNegZ(pos, tex, verts, uvs, normals, tris);
        }
    }
    private static void finishFace(Vector3 normalDir, List<Vector3> normals, List<int> tris)
    {
        int faceId = normals.Count / 4;
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);
        tris.Add(faceId * 4 + 2);
        tris.Add(faceId * 4 + 1);
        tris.Add(faceId * 4);
        tris.Add(faceId * 4);
        tris.Add(faceId * 4 + 3);
        tris.Add(faceId * 4 + 2);
    }
    //facing the +z direction
    private static void addFacePosZ(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(0, 0, 0));

        uvs.Add(new Vector2(info.UVMax[(int)Direction.PosZ].X, info.UVMin[(int)Direction.PosZ].Y));
        uvs.Add(info.UVMin[(int)Direction.PosZ]);
        uvs.Add(new Vector2(info.UVMin[(int)Direction.PosZ].X, info.UVMax[(int)Direction.PosZ].Y));
        uvs.Add(info.UVMax[(int)Direction.PosZ]);

        finishFace(new Vector3(0, 0, 1), normals, tris);
    }
    //facing the -z direction
    private static void addFaceNegZ(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 1));

        uvs.Add(new Vector2(info.UVMin[(int)Direction.NegZ].X, info.UVMax[(int)Direction.NegZ].Y));
        uvs.Add(info.UVMax[(int)Direction.NegZ]);
        uvs.Add(new Vector2(info.UVMax[(int)Direction.NegZ].X, info.UVMin[(int)Direction.NegZ].Y));
        uvs.Add(info.UVMin[(int)Direction.NegZ]);

        finishFace(new Vector3(0, 0, -1), normals, tris);
    }
    //facing the +x direction
    private static void addFacePosX(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 0, 0));

        uvs.Add(info.UVMax[(int)Direction.PosX]);
        uvs.Add(new Vector2(info.UVMax[(int)Direction.PosX].X, info.UVMin[(int)Direction.PosX].Y)); //2
        uvs.Add(info.UVMin[(int)Direction.PosX]);
        uvs.Add(new Vector2(info.UVMin[(int)Direction.PosX].X, info.UVMax[(int)Direction.PosX].Y)); //3

        finishFace(new Vector3(1, 0, 0), normals, tris);
    }
    //facing the -x direction
    private static void addFaceNegX(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 0, 1));

        uvs.Add(info.UVMax[(int)Direction.NegX]);
        uvs.Add(new Vector2(info.UVMax[(int)Direction.NegX].X, info.UVMin[(int)Direction.NegX].Y));
        uvs.Add(info.UVMin[(int)Direction.NegX]);
        uvs.Add(new Vector2(info.UVMin[(int)Direction.NegX].X, info.UVMax[(int)Direction.NegX].Y));

        finishFace(new Vector3(-1, 0, 0), normals, tris);
    }
    //facing the +y direction
    private static void addFacePosY(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 0));

        uvs.Add(info.UVMin[(int)Direction.PosY]);
        uvs.Add(new Vector2(info.UVMin[(int)Direction.PosY].X, info.UVMax[(int)Direction.PosY].Y));
        uvs.Add(info.UVMax[(int)Direction.PosY]);
        uvs.Add(new Vector2(info.UVMax[(int)Direction.PosY].X, info.UVMin[(int)Direction.PosY].Y));

        finishFace(new Vector3(0, 1, 0), normals, tris);
    }
    //facing the -y direction
    private static void addFaceNegY(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(0, 0, 1));

        uvs.Add(info.UVMin[(int)Direction.NegY]);
        uvs.Add(new Vector2(info.UVMax[(int)Direction.NegY].X, info.UVMin[(int)Direction.NegY].Y));
        uvs.Add(info.UVMax[(int)Direction.NegY]);
        uvs.Add(new Vector2(info.UVMin[(int)Direction.NegY].X, info.UVMax[(int)Direction.NegY].Y));

        finishFace(new Vector3(0, -1, 0), normals, tris);
    }
}
