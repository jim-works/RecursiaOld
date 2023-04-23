using System.IO;
namespace Recursia;
public interface ISerializable
{
    bool NoSerialize => false;
    void Serialize(BinaryWriter bw);
    void Deserialize(BinaryReader br);
}