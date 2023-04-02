using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System;

public partial class ChunkMesh
{
    private Godot.Collections.Array data;
    private ArrayMesh arrayMesh;

    public List<Vector3> Verts {get; private set;} = new List<Vector3>();
    public List<int> Tris {get; private set;} = new List<int>();
    public List<Vector3> Norms {get; private set;} = new List<Vector3>();
    public List<Vector2> UVs {get; private set;} = new List<Vector2>();

    public MeshInstance3D Node {get; private set;}

    public ulong Timestamp = 0;

    public ChunkMesh()
    {
        //setup mesh array
        data = new Godot.Collections.Array();
        arrayMesh = new ArrayMesh();
        data.Resize((int)ArrayMesh.ArrayType.Max);
    }
    public void ApplyTo(MeshInstance3D node, Material mat)
    {
        //TODO: make this more efficient
        data[(int)ArrayMesh.ArrayType.Vertex] = Variant.CreateFrom(new Span<Vector3>(Verts.ToArray()));
        data[(int)ArrayMesh.ArrayType.Index] = Variant.CreateFrom(new Span<int>(Tris.ToArray()));
        data[(int)ArrayMesh.ArrayType.Normal] = Variant.CreateFrom(new Span<Vector3>(Norms.ToArray()));
        data[(int)ArrayMesh.ArrayType.TexUV] = Variant.CreateFrom(new Span<Vector2>(UVs.ToArray()));
        arrayMesh.ClearSurfaces();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data);
        node.Mesh = arrayMesh;
        node.SetSurfaceOverrideMaterial(0, mat);
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