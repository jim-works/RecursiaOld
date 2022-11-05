using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class ChunkMesh
{
    private Array data;
    private ArrayMesh arrayMesh;

    public List<Vector3> Verts {get; private set;} = new List<Vector3>();
    public List<int> Tris {get; private set;} = new List<int>();
    public List<Vector3> Norms {get; private set;} = new List<Vector3>();
    public List<Vector2> UVs {get; private set;} = new List<Vector2>();

    public MeshInstance Node {get; private set;}

    public ChunkMesh()
    {
        //setup mesh array
        data = new Array();
        arrayMesh = new ArrayMesh();
        data.Resize((int)ArrayMesh.ArrayType.Max);
    }
    public void ApplyTo(MeshInstance node, Material mat)
    {
        data[(int)ArrayMesh.ArrayType.Vertex] = Verts;
        data[(int)ArrayMesh.ArrayType.Index] = Tris;
        data[(int)ArrayMesh.ArrayType.Normal] = Norms;
        data[(int)ArrayMesh.ArrayType.TexUv] = UVs;
        arrayMesh.ClearSurfaces();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data);
        node.Mesh = arrayMesh;
        node.SetSurfaceMaterial(0, mat);
        Node = node;
    }
    public void ClearData()
    {
        Verts.Clear();
        Tris.Clear();
        Norms.Clear();
        UVs.Clear();
        Node?.QueueFree();
        Node = null;
    }

}