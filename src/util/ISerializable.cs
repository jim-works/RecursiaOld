using System.IO;
namespace Recursia;
public interface ISerializable
{
    void Serialize(BinaryWriter bw);
    void Deserialize(BinaryReader br);
}