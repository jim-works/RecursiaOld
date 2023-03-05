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
    public static T[] DeserializeArray<T>(BinaryReader br, System.Func<BinaryReader, T> deserializer) {
        T[] arr = new T[br.ReadInt32()];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = deserializer(br);
        }
        return arr;
    }
}