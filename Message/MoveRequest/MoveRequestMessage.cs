using Unity.Mathematics;
using DOTSNET;

public struct MoveRequestMessage : NetworkMessage
{
    // client needs to identify the entity by netId
    public ulong netId;

    // Inputs
    public bool moveForward;
    public bool moveBackward;
    public bool moveRight;
    public bool moveLeft;
    public bool jump;
    public bool shift;

    // camera direction
    public float3 cameraForward;

    public byte GetID() => 0x66;

    public MoveRequestMessage(ulong netId,
        bool moveForward, bool moveBackward, bool moveRight, bool moveLeft,
        bool jump, bool shift,
        float3 cameraForward)
    {
        this.netId = netId;
        this.moveForward = moveForward;
        this.moveBackward = moveBackward;
        this.moveRight = moveRight;
        this.moveLeft = moveLeft;
        this.jump = jump;
        this.shift = shift;
        this.cameraForward = cameraForward;
    }

    public bool Serialize(ref BitWriter writer) =>
        writer.WriteULong(netId) &&
        writer.WriteBool(moveForward) &&
        writer.WriteBool(moveBackward) &&
        writer.WriteBool(moveRight) &&
        writer.WriteBool(moveLeft) &&
        writer.WriteBool(jump) &&
        writer.WriteBool(shift) &&
        writer.WriteFloat(cameraForward.x) &&
        writer.WriteFloat(cameraForward.y) &&
        writer.WriteFloat(cameraForward.z);

    public bool Deserialize(ref BitReader reader) =>
        reader.ReadULong(out netId) &&
        reader.ReadBool(out moveForward) &&
        reader.ReadBool(out moveBackward) &&
        reader.ReadBool(out moveRight) &&
        reader.ReadBool(out moveLeft) &&
        reader.ReadBool(out jump) &&
        reader.ReadBool(out shift) &&
        reader.ReadFloat(out cameraForward.x) &&
        reader.ReadFloat(out cameraForward.y) &&
        reader.ReadFloat(out cameraForward.z);
}

