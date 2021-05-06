using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Used to add <see cref="CameraFollowComponent"/> via the Editor.
[DisallowMultipleComponent]
public class CameraAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Yaw = 0.0f;
    public float YawSpeed = 2.5f;

    public float Pitch = 30.0f;
    public float PitchSpeed = 2.5f;
    public float MinPitch = 5.0f;
    public float MaxPitch = 85.0f;

    public float Zoom = 5.0f;
    public float ZoomSpeed = 2.5f;
    public float MinZoom = 1.0f;
    public float MaxZoom = 10.0f;

    public float CameraYOffset = 1.5f;

    public bool InvertYAxis = false;

    public bool UseLerp = true;
    public float LerpSpeed = 25.0f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (!enabled)
        {
            return;
        }

        dstManager.AddComponentData(entity, new CameraComponent()
        {
            MinPitch = MinPitch,
            MaxPitch = MaxPitch,
            MinZoom = MinZoom,
            MaxZoom = MaxZoom,
            Yaw = Yaw,
            YawSpeed = YawSpeed,
            Pitch = Pitch,
            PitchSpeed = PitchSpeed,
            Zoom = Zoom,
            ZoomSpeed = ZoomSpeed,
            CameraYOffset = CameraYOffset,
            InvertYAxis = InvertYAxis,
            UseLerp = UseLerp,
            LerpSpeed = LerpSpeed
        });
    }
}
