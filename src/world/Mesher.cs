using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class Mesher : Node
{
    [Export]
    public Material ChunkMaterial;

    private TextureManager textureManager = new TextureManager();
    
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var mesh = GenerateMesh(new Chunk());
        GetParent().CallDeferred("add_child", mesh);
    }
    public MeshInstance GenerateMesh(Chunk chunk)
    {
        if (chunk == null) {
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
        TextureInfo tex = textureManager.GetTexture("grass");
        addFacePosX(new Vector3(0,0,0), tex, vertices, uvs, normals, tris);
        addFacePosY(new Vector3(0,0,0), tex, vertices, uvs, normals, tris);
        addFacePosZ(new Vector3(0,0,0), tex, vertices, uvs, normals, tris);
        addFaceNegX(new Vector3(0,0,0), tex, vertices, uvs, normals, tris);
        addFaceNegY(new Vector3(0,0,0), tex, vertices, uvs, normals, tris);
        addFaceNegZ(new Vector3(0,0,0), tex, vertices, uvs, normals, tris);

        array[(int)ArrayMesh.ArrayType.Vertex] = vertices;
        array[(int)ArrayMesh.ArrayType.Index] = tris;
        array[(int)ArrayMesh.ArrayType.Normal] = normals;
        array[(int)ArrayMesh.ArrayType.TexUv] = uvs;
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

        mesh.Mesh = arrMesh;
        mesh.SetSurfaceMaterial(0, ChunkMaterial);

        return mesh;
    }
    private void finishFace(TextureInfo info, Vector3 normalDir, List<Vector2> uvs, List<Vector3> normals, List<int> tris) {
        int faceId = normals.Count/4;
        uvs.Add(info.Min);
        uvs.Add(new Vector2(info.Max.x,info.Min.y));
        uvs.Add(info.Max);
        uvs.Add(new Vector2(info.Min.x,info.Max.y));
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);
        tris.Add(faceId*4+2);
        tris.Add(faceId*4+1);
        tris.Add(faceId*4);
        tris.Add(faceId*4);
        tris.Add(faceId*4+3);
        tris.Add(faceId*4+2);
    }
    private void addFacePosZ(Vector3 origin, TextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0,1,0));
        verts.Add(origin + new Vector3(1,1,0));
        verts.Add(origin + new Vector3(1,0,0));
        verts.Add(origin + new Vector3(0,0,0));
        finishFace(texInfo, new Vector3(0,0,1), uvs, normals, tris);
    }
    private void addFaceNegZ(Vector3 origin, TextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0,0,1));
        verts.Add(origin + new Vector3(1,0,1));
        verts.Add(origin + new Vector3(1,1,1));
        verts.Add(origin + new Vector3(0,1,1));
        finishFace(texInfo, new Vector3(0,0,-1), uvs, normals, tris);
    }
    private void addFacePosX(Vector3 origin, TextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0,0,1));
        verts.Add(origin + new Vector3(0,1,1));
        verts.Add(origin + new Vector3(0,1,0));
        verts.Add(origin + new Vector3(0,0,0));
        finishFace(texInfo, new Vector3(1,0,0), uvs, normals, tris);
    }
    private void addFaceNegX(Vector3 origin, TextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(1,0,0));
        verts.Add(origin + new Vector3(1,1,0));
        verts.Add(origin + new Vector3(1,1,1));
        verts.Add(origin + new Vector3(1,0,1));
        finishFace(texInfo, new Vector3(-1,0,0), uvs, normals, tris);
    }
    private void addFacePosY(Vector3 origin, TextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0,1,0));
        verts.Add(origin + new Vector3(0,1,1));
        verts.Add(origin + new Vector3(1,1,1));
        verts.Add(origin + new Vector3(1,1,0));
        
        finishFace(texInfo, new Vector3(0,1,0), uvs, normals, tris);
    }
    private void addFaceNegY(Vector3 origin, TextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0,0,0));
        verts.Add(origin + new Vector3(1,0,0));
        verts.Add(origin + new Vector3(1,0,1));
        verts.Add(origin + new Vector3(0,0,1));
        finishFace(texInfo, new Vector3(0,-1,0), uvs, normals, tris);
    }
}
