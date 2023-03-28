using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
public partial class Mesher : Node
{
    [Export]
    public Material ChunkMaterial;
    [Export]
    public float MaxMeshTime = 0.1f;
    public static Mesher Singleton;
    private HashSet<Chunk> toMesh = new HashSet<Chunk>();
    private Dictionary<ChunkCoord, Chunk> waitingToMesh = new();
    private ConcurrentBag<(ChunkMesh, ChunkCoord)> finishedMeshes = new ConcurrentBag<(ChunkMesh, ChunkCoord)>();
    //private Pool<ChunkMesh> meshPool = new Pool<ChunkMesh>(() => new ChunkMesh(), m => m.Node != null, m => m.ClearData(), 100);
    // Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
        Singleton = this;
        GD.Print("Mesher initialized!");
        base._EnterTree();
    }
    public override void _Ready()
    {
        World.Singleton.OnChunkUnload += Unload;
        World.Singleton.OnChunkUpdate += OnChunkUpdate;
        World.Singleton.OnChunkReady += OnChunkReady;
        GD.Print("Mesher initialized!");
        base._Ready();
    }
    public override void _Process(double delta)
    {
        //multithread chunk generation
        foreach (var mesghin in toMesh)
        {
            multithreadGenerateChunk(mesghin);
        }
        toMesh.Clear();
        //spawn all on single thread to avoid a million race conditions
        while (finishedMeshes.Count > 0)
        {
            if (finishedMeshes.TryTake(out (ChunkMesh, ChunkCoord) pair))
            {
                spawnChunk(pair.Item1, pair.Item2);
            }
        }
        base._Process(delta);
    }
    public void Unload(Chunk chunk)
    {
        if (chunk == null) return;
        chunk.Mesh?.ClearData();
        chunk.GenerationState = ChunkGenerationState.GENERATED;
        chunk.Mesh = null;
    }
    public void OnChunkReady(Chunk c)
    {
        if (canMesh(c)) {
            MeshDeferred(c);
            waitingToMesh.Remove(c.Position);
        }
        else waitingToMesh[c.Position] = c;
        

        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(1,0,0), out Chunk c1) && canMesh(c1)) {
            MeshDeferred(c1);
            waitingToMesh.Remove(c1.Position);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(-1,0,0), out Chunk c2) && canMesh(c2)) {
            MeshDeferred(c2);
            waitingToMesh.Remove(c2.Position);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,1,0), out Chunk c3) && canMesh(c3)) {
            MeshDeferred(c3);
            waitingToMesh.Remove(c3.Position);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,-1,0), out Chunk c4) && canMesh(c4)) {
            MeshDeferred(c4);
            waitingToMesh.Remove(c4.Position);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,0,1), out Chunk c5) && canMesh(c5)) {
            MeshDeferred(c5);
            waitingToMesh.Remove(c5.Position);
        }
        if (waitingToMesh.TryGetValue(c.Position + new ChunkCoord(0,0,-1), out Chunk c6) && canMesh(c6)) {
            MeshDeferred(c6);
            waitingToMesh.Remove(c6.Position);
        }
    }
    public void OnChunkUpdate(Chunk c)
    {
        MeshDeferred(c);
        if (World.Singleton.Chunks.TryGetValue(c.Position + new ChunkCoord(1,0,0), out Chunk c1)) MeshDeferred(c1);
        if (World.Singleton.Chunks.TryGetValue(c.Position + new ChunkCoord(-1,0,0), out Chunk c2)) MeshDeferred(c2);
        if (World.Singleton.Chunks.TryGetValue(c.Position + new ChunkCoord(0,1,0), out Chunk c3)) MeshDeferred(c3);
        if (World.Singleton.Chunks.TryGetValue(c.Position + new ChunkCoord(0,-1,0), out Chunk c4)) MeshDeferred(c4);
        if (World.Singleton.Chunks.TryGetValue(c.Position + new ChunkCoord(0,0,1), out Chunk c5)) MeshDeferred(c5);
        if (World.Singleton.Chunks.TryGetValue(c.Position + new ChunkCoord(0,0,-1), out Chunk c6)) MeshDeferred(c6);
    }
    private bool canMesh(Chunk c)
    {
        //only mesh if all adjacent chunks are generated
        return c != null && World.Singleton.GetChunk(c.Position + new ChunkCoord(1,0,0))?.GenerationState == ChunkGenerationState.GENERATED 
        && World.Singleton.GetChunk(c.Position + new ChunkCoord(-1,0,0))?.GenerationState == ChunkGenerationState.GENERATED 
        && World.Singleton.GetChunk(c.Position + new ChunkCoord(0,1,0))?.GenerationState == ChunkGenerationState.GENERATED 
        && World.Singleton.GetChunk(c.Position + new ChunkCoord(0,-1,0))?.GenerationState == ChunkGenerationState.GENERATED 
        && World.Singleton.GetChunk(c.Position + new ChunkCoord(0,0,1))?.GenerationState == ChunkGenerationState.GENERATED 
        && World.Singleton.GetChunk(c.Position + new ChunkCoord(0,0,-1))?.GenerationState == ChunkGenerationState.GENERATED ;
    }
    //queues a chunk to be meshed
    public void MeshDeferred(Chunk chunk) {
        toMesh.Add(chunk);
    }
    //applies mesh to chunk, removes old mesh if needed, spawns chunk in scene as a child as this node
    private void spawnChunk(ChunkMesh mesh, ChunkCoord coord)
    {
        Chunk chunk = World.Singleton.GetChunk(coord);
        if (chunk == null) return;
        chunk.Mesh?.ClearData();
        chunk.Mesh = mesh;
        if (mesh == null)
        {
            //no need to spawn in a new MeshInstance3D if the chunk is empty
            return;
        }
        MeshInstance3D meshNode = new MeshInstance3D();
        chunk.Mesh.ApplyTo(meshNode, ChunkMaterial);
        AddChild(chunk.Mesh.Node);
    }
    //places finished chunk in finishedMeshes
    private void multithreadGenerateChunk(Chunk chunk) {
        Chunk c = chunk;
        Task.Run(() => {
            ChunkMesh mesh = GenerateMesh(c);
            finishedMeshes.Add((mesh, c.Position));
        });
    }
    private ChunkMesh getMesh()
    {
        return new ChunkMesh();
    }
    private ChunkMesh GenerateMesh(Chunk chunk)
    {
        if (chunk == null)
        {
            return null;
        }
        ChunkMesh chunkMesh = getMesh();
        var vertices = chunkMesh.Verts;
        var tris = chunkMesh.Tris;
        var normals = chunkMesh.Norms;
        var uvs = chunkMesh.UVs;

        Chunk[] neighbors = new Chunk[6]; //we are in 3d
        neighbors[(int)Direction.PosX] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(1,0,0));
        neighbors[(int)Direction.PosY] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,1,0));
        neighbors[(int)Direction.PosZ] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,0,1));
        neighbors[(int)Direction.NegX] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(-1,0,0));
        neighbors[(int)Direction.NegY] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,-1,0));
        neighbors[(int)Direction.NegZ] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,0,-1));

        //generate the mesh
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (chunk[x,y,z] == null) continue;
                    AtlasTextureInfo tex = chunk[x,y,z].TextureInfo;
                    meshBlock(chunk, neighbors, new BlockCoord(x,y,z), tex, vertices, uvs, normals, tris);
                }
            }
        }

        if (vertices.Count==0) {
            chunkMesh.ClearData();
            return null;
        }
        return chunkMesh;
    }
    private void meshBlock(Chunk chunk, Chunk[] neighbors, BlockCoord localPos, AtlasTextureInfo tex, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        bool nonOpaque(Chunk c, int x, int y, int z) => c == null || c[x,y,z] == null || c[x,y,z].Transparent;
        Vector3 pos = (Vector3)chunk.LocalToWorld(localPos);

        //check if there's no block/a transparent block in each direction. only generate face if so.
        if (localPos.X == 0 && nonOpaque(neighbors[(int)Direction.NegX], (int)Chunk.CHUNK_SIZE-1,localPos.Y,localPos.Z) || localPos.X != 0 && nonOpaque(chunk,localPos.X-1,localPos.Y,localPos.Z)) {
            addFacePosX(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Y == 0 && nonOpaque(neighbors[(int)Direction.NegY], localPos.X, (int)Chunk.CHUNK_SIZE-1,localPos.Z) || localPos.Y != 0 && nonOpaque(chunk,localPos.X,localPos.Y-1,localPos.Z)) {
            addFaceNegY(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Z == 0 && nonOpaque(neighbors[(int)Direction.NegZ], localPos.X,localPos.Y,(int)Chunk.CHUNK_SIZE-1) || localPos.Z != 0 && nonOpaque(chunk,localPos.X,localPos.Y,localPos.Z-1)) {
            addFacePosZ(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.X == Chunk.CHUNK_SIZE-1 && nonOpaque(neighbors[(int)Direction.PosX], 0,localPos.Y,localPos.Z) || localPos.X != Chunk.CHUNK_SIZE-1 && nonOpaque(chunk,localPos.X+1,localPos.Y,localPos.Z)) {
            addFaceNegX(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Y == Chunk.CHUNK_SIZE-1 && nonOpaque(neighbors[(int)Direction.PosY], localPos.X,0,localPos.Z) || localPos.Y != Chunk.CHUNK_SIZE-1 && nonOpaque(chunk,localPos.X,localPos.Y+1,localPos.Z)) {
            addFacePosY(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.Z == Chunk.CHUNK_SIZE-1 && nonOpaque(neighbors[(int)Direction.PosZ], localPos.X,localPos.Y,0) || localPos.Z != Chunk.CHUNK_SIZE-1 && nonOpaque(chunk,localPos.X,localPos.Y,localPos.Z+1)) {
            addFaceNegZ(pos, tex, verts, uvs, normals, tris);
        }
    }
    private void finishFace(AtlasTextureInfo info, Vector3 normalDir, List<Vector3> normals, List<int> tris)
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
    private void addFacePosZ(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(0, 0, 0));

        uvs.Add(new Vector2(info.UVMax.X, info.UVMin.Y));
        uvs.Add(info.UVMin);
        uvs.Add(new Vector2(info.UVMin.X, info.UVMax.Y));
        uvs.Add(info.UVMax);

        finishFace(info, new Vector3(0, 0, 1), normals, tris);
    }
    //facing the -z direction
    private void addFaceNegZ(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 1));
        
        uvs.Add(new Vector2(info.UVMin.X, info.UVMax.Y));
        uvs.Add(info.UVMax);
        uvs.Add(new Vector2(info.UVMax.X, info.UVMin.Y));
        uvs.Add(info.UVMin);
        
        finishFace(info, new Vector3(0, 0, -1), normals, tris);
    }
    //facing the +x direction
    private void addFacePosX(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 0, 0));
        
        uvs.Add(info.UVMax);
        uvs.Add(new Vector2(info.UVMax.X, info.UVMin.Y)); //2
        uvs.Add(info.UVMin);
        uvs.Add(new Vector2(info.UVMin.X, info.UVMax.Y)); //3


        finishFace(info, new Vector3(1, 0, 0), normals, tris);
    }
    //facing the -x direction
    private void addFaceNegX(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 0, 1));
        
        uvs.Add(info.UVMax);
        uvs.Add(new Vector2(info.UVMax.X, info.UVMin.Y));
        uvs.Add(info.UVMin);
        uvs.Add(new Vector2(info.UVMin.X, info.UVMax.Y));
        

        finishFace(info, new Vector3(-1, 0, 0), normals, tris);
    }
    //facing the +y direction
    private void addFacePosY(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 0));

        uvs.Add(info.UVMin);
        uvs.Add(new Vector2(info.UVMin.X, info.UVMax.Y));
        uvs.Add(info.UVMax);
        uvs.Add(new Vector2(info.UVMax.X, info.UVMin.Y));

        finishFace(info, new Vector3(0, 1, 0), normals, tris);
    }
    //facing the -y direction
    private void addFaceNegY(Vector3 origin, AtlasTextureInfo info, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(0, 0, 1));

        uvs.Add(info.UVMin);
        uvs.Add(new Vector2(info.UVMax.X, info.UVMin.Y));
        uvs.Add(info.UVMax);
        uvs.Add(new Vector2(info.UVMin.X, info.UVMax.Y));

        finishFace(info, new Vector3(0, -1, 0), normals, tris);
    }
}
