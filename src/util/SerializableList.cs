using System.Collections.Generic;
using System.IO;

namespace Recursia;
public class SerializableList<T> : ISerializable where T : ISerializable, new()
{
    public List<T> List = new();
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(List.Count);
        foreach (var item in List)
        {
            item.Serialize(bw);
        }
    }
    public void Deserialize(BinaryReader br)
    {
        int size = br.ReadInt32();
        List.Clear();
        for (int i = 0; i < size; i++)
        {
            T item=new();
            item.Deserialize(br);
            List.Add(item);
        }
    }
}