using Godot;
using System.IO;

public static class SerializationExtensions
{
    public static void Serialize(this Vector3 v, BinaryWriter bw)
    {
        bw.Write(v.x);
        bw.Write(v.y);
        bw.Write(v.z);
    }
    public static void Deserialize(this Vector3 v, BinaryReader br)
    {
        v.x = br.ReadSingle();
        v.y = br.ReadSingle();
        v.z = br.ReadSingle();
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