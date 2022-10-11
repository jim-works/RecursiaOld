using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class Mesher : Node
{
    [Export]
    public Material ChunkMaterial;

    private World world;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        BlockLoader.Load();
        world = new World();
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++) {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++) {
                world.SetBlock(new Int3(x, 0, z), BlockTypes.Get("stone"));
            }
        }
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++) {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++) {
                world.SetBlock(new Int3(x, 1, z), BlockTypes.Get("dirt"));
            }
        }
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++) {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++) {
                world.SetBlock(new Int3(x, 2, z), BlockTypes.Get("grass"));
            }
        }
        var mesh = GenerateMesh(world.Chunks[new Int3(0,0,0)]);

        GetParent().CallDeferred("add_child", mesh);
    }
    public MeshInstance GenerateMesh(Chunk chunk)
    {
        if (chunk == null)
        {
            return null;
        }
        var mesh = new MeshInstance();
        var arrMesh = new ArrayMesh();

        Array array = new Array();
        array.Resize((int)ArrayMesh.ArrayType.Max);
        var vertices = new List<Vector3>();
        var tris = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();

        //generate the mesh
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (chunk[x,y,z] == null) continue;
                    BlockTextureInfo tex = chunk[x,y,z].TextureInfo;
                    Vector3 pos = (Vector3)chunk.LocalToWorld(new Int3(x,y,z));
                    addFacePosX(pos, tex, vertices, uvs, normals, tris);
                    addFacePosY(pos, tex, vertices, uvs, normals, tris);
                    addFacePosZ(pos, tex, vertices, uvs, normals, tris);
                    addFaceNegX(pos, tex, vertices, uvs, normals, tris);
                    addFaceNegY(pos, tex, vertices, uvs, normals, tris);
                    addFaceNegZ(pos, tex, vertices, uvs, normals, tris);
                }
            }
        }


        array[(int)ArrayMesh.ArrayType.Vertex] = vertices;
        array[(int)ArrayMesh.ArrayType.Index] = tris;
        array[(int)ArrayMesh.ArrayType.Normal] = normals;
        array[(int)ArrayMesh.ArrayType.TexUv] = uvs;
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

        mesh.Mesh = arrMesh;
        mesh.SetSurfaceMaterial(0, ChunkMaterial);

        return mesh;
    }
    private void finishFace(BlockTextureInfo info, Vector3 normalDir, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        int faceId = normals.Count / 4;
        uvs.Add(info.UVMin);
        uvs.Add(new Vector2(info.UVMax.x, info.UVMin.y));
        uvs.Add(info.UVMax);
        uvs.Add(new Vector2(info.UVMin.x, info.UVMax.y));
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
    private void addFacePosZ(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(0, 0, 0));
        finishFace(texInfo, new Vector3(0, 0, 1), uvs, normals, tris);
    }
    private void addFaceNegZ(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 1));
        finishFace(texInfo, new Vector3(0, 0, -1), uvs, normals, tris);
    }
    private void addFacePosX(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 0, 0));
        finishFace(texInfo, new Vector3(1, 0, 0), uvs, normals, tris);
    }
    private void addFaceNegX(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 0, 1));
        finishFace(texInfo, new Vector3(-1, 0, 0), uvs, normals, tris);
    }
    private void addFacePosY(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 0));

        finishFace(texInfo, new Vector3(0, 1, 0), uvs, normals, tris);
    }
    private void addFaceNegY(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(0, 0, 1));
        finishFace(texInfo, new Vector3(0, -1, 0), uvs, normals, tris);
    }
}
