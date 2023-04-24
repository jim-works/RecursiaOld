using System.IO;
namespace Recursia;
public interface ISerializable
{
    //TODO: I don't think I need this
    bool NoSerialize() => false;
    void Serialize(BinaryWriter bw);
    void Deserialize(BinaryReader br);
}