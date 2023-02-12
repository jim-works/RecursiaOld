using System.IO;

public interface ISerializable
{
    void Serialize(BinaryWriter bw);
}