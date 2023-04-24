using Godot;
using System;
using System.IO;

namespace Recursia;
public static class SerializationExtensions
{
    public static void Serialize(this Vector3 v, BinaryWriter bw)
    {
        bw.Write(v.X);
        bw.Write(v.Y);
        bw.Write(v.Z);
    }
    public static Vector3 DeserializeVec3(BinaryReader br)
    {
        Vector3 v;
        v.X = br.ReadSingle();
        v.Y = br.ReadSingle();
        v.Z = br.ReadSingle();
        return v;
    }
    public static void Serialize(Block?[,,]? blocks, BinaryWriter bw)
    {
        if (blocks == null)
        {
            //this case doesn't need to exist, but should be faster than the other
            bw.Write(Chunk.CHUNK_SIZE*Chunk.CHUNK_SIZE*Chunk.CHUNK_SIZE);
            SerializeBlock(bw,null);
        }
        else
        {
            //TODO: change to span and use stackalloc
            Block?[,,] saving = new Block[Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE];
            Array.Copy(blocks, saving, blocks.Length);
            Block? curr = saving[0, 0, 0];
            int run = 0;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        Block? b = saving[x, y, z];
                        if (curr == b)
                        {
                            run++;
                            continue;
                        }
                        bw.Write(run);
                        SerializeBlock(bw, curr);
                        run = 1;
                        curr = b;
                    }
                }
            }

            bw.Write(run);
            SerializeBlock(bw, curr);
        }
    }

    public static Block?[,,] DeserializeBlockArray(BinaryReader br)
    {
        Block?[,,] b = new Block[Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE];
        int run = 0;
        //deserialize blocks
        Block? read = null;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (run == 0)
                    {
                        run = br.ReadInt32();
                        try
                        {
                            read = DeserializeBlock(br);
                        }
                        catch (Exception e)
                        {
                            GD.PushError($"Error deserializing block {read?.Name ?? "null"} at {x} {y} {z}: {e}");
                            read = null;
                        }
                    }
                    b[x, y, z] = read;
                    run--;
                }
            }
        }
        return b;
    }
    public static Block? DeserializeBlock(BinaryReader br)
    {
        byte val = br.ReadByte();
        if (val == 0) return null;
        string blockName = br.ReadString();
        if(BlockTypes.TryGet(blockName, out Block? b))
        {
            b.Deserialize(br);
        }
        else
        {
            GD.PushWarning($"Couldn't get block type: {blockName}");
        }
        return b;
    }
    public static void SerializeBlock(BinaryWriter bw, Block? b)
    {
        if (b == null)
        {
            bw.Write((byte)0);
            return;
        }
        bw.Write((byte)1);
        b.Serialize(bw);
    }

    public static void Serialize<T>(this T[] arr, BinaryWriter bw) where T : ISerializable
    {
        bw.Write(arr.Length);
        foreach (var item in arr)
        {
            item.Serialize(bw);
        }
    }
    public static void SerializeMaybeNull<T>(T? inst, BinaryWriter bw) where T : ISerializable
    {
        bw.Write(inst is null ? (byte)0 : (byte)1);
        inst?.Serialize(bw);
    }
    //returns false if it read null, true otherwise
    public static bool DeserializeMaybeNull<T>(T inst, BinaryReader br) where T : ISerializable
    {
        byte val = br.ReadByte();
        if (val == 0) return false;
        inst.Deserialize(br);
        return true;
    }
    public static T[] DeserializeArray<T>(BinaryReader br, Func<BinaryReader, T> deserializer) {
        T[] arr = new T[br.ReadInt32()];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = deserializer(br);
        }
        return arr;
    }
    public static T[] DeserializeArray<T>(BinaryReader br) where T : ISerializable, new() {
        T[] arr = new T[br.ReadInt32()];
        for (int i = 0; i < arr.Length; i++)
        {
            T inst = new();
            inst.Deserialize(br);
            arr[i] = inst;
        }
        return arr;
    }
}