using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DOTSNET;

[DisallowMultipleComponent]
public class CharacterControllerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float3 Gravity = new float3(0.0f, -9.81f, 0.0f);
    public float MaxSpeed = 7.5f;
    public float Speed = 5.0f;
    public float RunFactor = 1.5f;
    public float JumpStrength = 0.15f;
    public float InAirFactor = 0.65f;
    public float MaxStep = 0.35f;
    public float Drag = 0.2f;
    public float RotationLerpSpeed = 0.2f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (!enabled && !GetComponent<NetworkEntity>().owned) { return; }

        dstManager.AddComponentData(entity, new CharacterControllerComponent()
        {
            Gravity = Gravity,
            MaxSpeed = MaxSpeed,
            Speed = Speed,
            RunFactor = RunFactor,
            JumpStrength = JumpStrength,
            InAirFactor = InAirFactor,
            MaxStep = MaxStep,
            Drag = Drag,
            RotationLerpSpeed = RotationLerpSpeed
        });
    }
}
