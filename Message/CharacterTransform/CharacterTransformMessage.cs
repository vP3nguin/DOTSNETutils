using Unity.Mathematics;
using DOTSNET;

public struct CharacterTransformMessage : NetworkMessage
{
    public ulong netId;

    public float3 position;
    public float3 rotation;

    public byte animationId;

    public byte GetID() => 0x67;

    public CharacterTransformMessage(ulong netId,
        float3 position, float3 rotation, byte animationId)
    {
        this.netId = netId;
        this.position = position;
        this.rotation = rotation;
        this.animationId = animationId;
    }

    public bool Serialize(ref BitWriter writer) =>
        writer.WriteULong(netId) &&
        writer.WriteFloat(position.x) &&
        writer.WriteFloat(position.y) &&
        writer.WriteFloat(position.z) &&
        writer.WriteFloat(rotation.x) &&
        writer.WriteFloat(rotation.y) &&
        writer.WriteFloat(rotation.z) &&
        writer.WriteByte(animationId);

    public bool Deserialize(ref BitReader reader) =>
        reader.ReadULong(out netId) &&
        reader.ReadFloat(out position.x) &&
        reader.ReadFloat(out position.y) &&
        reader.ReadFloat(out position.z) &&
        reader.ReadFloat(out rotation.x) &&
        reader.ReadFloat(out rotation.y) &&
        reader.ReadFloat(out rotation.z) &&
        reader.ReadByte(out animationId);
}

