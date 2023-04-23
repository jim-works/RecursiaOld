using Godot;

namespace Recursia;
public class ChunkNodeData
{
    public ulong Timestamp;
    public ChunkMesh OpaqueMesh = new();
    public ChunkMesh TransparentMesh = new();

    public void ClearData()
    {
        OpaqueMesh.ClearData();
        TransparentMesh.ClearData();
    }
    public void Spawn(Node parent, Material? opaqueMat, Material? transparentMat)
    {
        if (OpaqueMesh.Verts.Count > 0) OpaqueMesh.Spawn(parent, opaqueMat);
        if (TransparentMesh.Verts.Count > 0) TransparentMesh.Spawn(parent,transparentMat);
    }
}