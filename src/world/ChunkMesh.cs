using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System;

namespace Recursia;
public sealed class ChunkMesh : IDisposable
{
    private readonly Godot.Collections.Array data;
    private readonly ArrayMesh arrayMesh;

    public List<Vector3> Verts {get;} = new List<Vector3>();
    public List<int> Tris {get;} = new List<int>();
    public List<Vector3> Norms {get;} = new List<Vector3>();
    public List<Vector2> UVs {get;} = new List<Vector2>();

    public MeshInstance3D Node {get;private set;}

    public ulong Timestamp;

    public ChunkMesh()
    {
        //setup mesh array
        data = new Godot.Collections.Array();
        arrayMesh = new ArrayMesh();
        _ = data.Resize((int)Mesh.ArrayType.Max);
    }
    public void ApplyTo(MeshInstance3D node, Material mat)
    {
        //TODO: make this more efficient
        data[(int)Mesh.ArrayType.Vertex] = Variant.CreateFrom(new Span<Vector3>(Verts.ToArray()));
        data[(int)Mesh.ArrayType.Index] = Variant.CreateFrom(new Span<int>(Tris.ToArray()));
        data[(int)Mesh.ArrayType.Normal] = Variant.CreateFrom(new Span<Vector3>(Norms.ToArray()));
        data[(int)Mesh.ArrayType.TexUV] = Variant.CreateFrom(new Span<Vector2>(UVs.ToArray()));
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

    public void Dispose()
    {
        arrayMesh.Dispose();
        data.Dispose();
    }
}