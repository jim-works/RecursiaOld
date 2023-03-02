using Godot;
using System.IO;

public static class SerializationExtensions
{
    public static void Serialize(this Vector3 v, BinaryWriter bw)
    {
        bw.Write(v.X);
        bw.Write(v.Y);
        bw.Write(v.Z);
    }
    public static void Deserialize(this Vector3 v, BinaryReader br)
    {
        v.X = br.ReadSingle();
        v.Y = br.ReadSingle();
        v.Z = br.ReadSingle();
    }

    public static void Serialize<T>(this T[] arr, BinaryWriter bw) where T : ISerializable
    {
        bw.Write(arr.Length);
        foreach (var item in arr)
        {
            item.Serialize(bw);
        }
    }
}