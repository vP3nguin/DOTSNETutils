using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DOTSNET;
using System;
using Unity.Physics;

[DisallowMultipleComponent]
public class PlayerServerSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(PlayerServerSystem);
}

[ServerWorld]
[UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
[DisableAutoCreation]
public class PlayerServerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        RemovePhysics();
    }

    public void RemovePhysics()
    {
        // remove physics components from spheres on the server,
        // so that we can apply NetworkTransform synchronization.
        Entities.ForEach((in Entity entity, in PlayerComponent player) =>
        {
            EntityManager.RemoveComponent<PhysicsCollider>(entity);
            EntityManager.RemoveComponent<PhysicsDamping>(entity);
            EntityManager.RemoveComponent<PhysicsGravityFactor>(entity);
            EntityManager.RemoveComponent<PhysicsMass>(entity);
            EntityManager.RemoveComponent<PhysicsVelocity>(entity);
        })
        .WithStructuralChanges()
        .Run();
    }
}
