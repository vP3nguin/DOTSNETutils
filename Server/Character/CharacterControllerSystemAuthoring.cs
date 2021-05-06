using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using DOTSNET;
using UnityEngine;
using System;
using Unity.Physics;
using Unity.Physics.Systems;

public class CharacterControllerSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(CharacterControllerSystem);
}

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
[DisableAutoCreation]
public class CharacterControllerSystem : JobComponentSystem
{
    private const float Epsilon = 0.001f;

    private BuildPhysicsWorld buildPhysicsWorld;
    private ExportPhysicsWorld exportPhysicsWorld;
    private EndFramePhysicsSystem endFramePhysicsSystem;

    private EntityQuery characterControllerGroup;

    protected override void OnCreate()
    {
        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        exportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
        endFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

        characterControllerGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                    typeof(CharacterControllerComponent),
                    typeof(Translation),
                    typeof(Rotation),
                    typeof(NetworkEntity),
                    typeof(PhysicsCollider)
            }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (characterControllerGroup.CalculateChunkCount() == 0)
        {
            return inputDeps;
        }

        var entityTypeHandle = GetEntityTypeHandle();
        var colliderData = GetComponentDataFromEntity<PhysicsCollider>(true);
        var characterControllerTypeHandle = GetComponentTypeHandle<CharacterControllerComponent>();
        var translationTypeHandle = GetComponentTypeHandle<Translation>();
        var rotationTypeHandle = GetComponentTypeHandle<Rotation>();
        var netEntityTypeHandle = GetComponentTypeHandle<NetworkEntity>();

        var controllerJob = new CharacterControllerJob()
        {
            DeltaTime = Time.DeltaTime,

            PhysicsWorld = buildPhysicsWorld.PhysicsWorld,
            EntityHandles = entityTypeHandle,
            ColliderData = colliderData,
            CharacterControllerHandles = characterControllerTypeHandle,
            TranslationHandles = translationTypeHandle,
            RotationHandles = rotationTypeHandle,
            NetEntityHandles = netEntityTypeHandle
        };

        var dependency = JobHandle.CombineDependencies(inputDeps, exportPhysicsWorld.GetOutputDependency());
        var controllerJobHandle = controllerJob.ScheduleParallel(characterControllerGroup, dependency);

        endFramePhysicsSystem.AddInputDependency(controllerJobHandle);

        return controllerJobHandle;
    }

    // The job that performs all of the logic of the character controller.
    [BurstCompile]
    private struct CharacterControllerJob : IJobChunk
    {
        public float DeltaTime;

        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public EntityTypeHandle EntityHandles;
        [ReadOnly] public ComponentTypeHandle<NetworkEntity> NetEntityHandles;
        [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> ColliderData;

        public ComponentTypeHandle<CharacterControllerComponent> CharacterControllerHandles;
        public ComponentTypeHandle<Translation> TranslationHandles;
        public ComponentTypeHandle<Rotation> RotationHandles;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var collisionWorld = PhysicsWorld.CollisionWorld;

            var chunkEntityData = chunk.GetNativeArray(EntityHandles);
            var chunkCharacterControllerData = chunk.GetNativeArray(CharacterControllerHandles);
            var chunkTranslationData = chunk.GetNativeArray(TranslationHandles);
            var chunkRotationData = chunk.GetNativeArray(RotationHandles);
            var chunkNetEntityData = chunk.GetNativeArray(NetEntityHandles);

            for (int i = 0; i < chunk.Count; ++i)
            {
                var entity = chunkEntityData[i];
                var controller = chunkCharacterControllerData[i];
                var position = chunkTranslationData[i];
                var rotation = chunkRotationData[i];
                var collider = ColliderData[entity];
                var netentity = chunkNetEntityData[i];

                HandleChunk(ref entity, ref controller, ref position, ref rotation, ref collider, ref collisionWorld, ref netentity);

                chunkTranslationData[i] = position;
                chunkCharacterControllerData[i] = controller;
            }
        }

        // Processes a specific entity in the chunk.
        private void HandleChunk(ref Entity entity, ref CharacterControllerComponent controller, ref Translation position, ref Rotation rotation, ref PhysicsCollider collider, ref CollisionWorld collisionWorld, ref NetworkEntity netentity)
        {
            if (netentity.owned)
            {
                float3 epsilon = new float3(0.0f, Epsilon, 0.0f) * -math.normalize(controller.Gravity);
                float3 currPos = position.Value + epsilon;
                quaternion currRot = rotation.Value;

                float3 gravityVelocity = controller.Gravity * DeltaTime;
                float3 verticalVelocity = (controller.VerticalVelocity + gravityVelocity);
                float3 horizontalVelocity = (controller.CurrentDirection * controller.CurrentMagnitude * controller.Speed * DeltaTime);

                if (controller.IsGrounded)
                {
                    if (controller.Jump)
                    {
                        verticalVelocity = controller.JumpStrength * -math.normalize(controller.Gravity);
                    }
                    else
                    {
                        float3 gravityDir = math.normalize(gravityVelocity);
                        float3 verticalDir = math.normalize(verticalVelocity);

                        if (MathUtils.FloatEquals(math.dot(gravityDir, verticalDir), 1.0f))
                        {
                            verticalVelocity = new float3();
                        }
                    }
                }

                HandleHorizontalMovement(ref horizontalVelocity, ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                currPos += horizontalVelocity;

                HandleVerticalMovement(ref verticalVelocity, ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                currPos += verticalVelocity;

                DetermineIfGrounded(entity, ref currPos, ref epsilon, ref controller, ref collider, ref collisionWorld);

                position.Value = currPos - epsilon;
            }
        }

        // Handles horizontal movement on the XZ plane.
        private void HandleHorizontalMovement(
            ref float3 horizontalVelocity,
            ref Entity entity,
            ref float3 currPos,
            ref quaternion currRot,
            ref CharacterControllerComponent controller,
            ref PhysicsCollider collider,
            ref CollisionWorld collisionWorld)
        {
            if (MathUtils.IsZero(horizontalVelocity))
            {
                return;
            }

            float3 targetPos = currPos + horizontalVelocity;

            NativeList<ColliderCastHit> horizontalCollisions = PhysicsUtils.ColliderCastAll(collider, currPos, targetPos, ref collisionWorld, entity, Allocator.Temp);
            PhysicsUtils.TrimByFilter(ref horizontalCollisions, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);

            if (horizontalCollisions.Length > 0)
            {
                //Debug.Log(horizontalCollisions.ToString());

                // We either have to step or slide as something is in our way.
                float3 step = new float3(0.0f, controller.MaxStep, 0.0f);
                PhysicsUtils.ColliderCast(out ColliderCastHit nearestStepHit, collider, targetPos + step, targetPos, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp);

                if (!MathUtils.IsZero(nearestStepHit.Fraction))
                {
                    // We can step up.
                    targetPos += (step * (1.0f - nearestStepHit.Fraction));
                    horizontalVelocity = targetPos - currPos;
                }
                else
                {
                    // We can not step up, so slide.
                    NativeList<DistanceHit> horizontalDistances = PhysicsUtils.ColliderDistanceAll(collider, 1.0f, new RigidTransform() { pos = currPos + horizontalVelocity, rot = currRot }, ref collisionWorld, entity, Allocator.Temp);
                    PhysicsUtils.TrimByFilter(ref horizontalDistances, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);

                    for (int i = 0; i < horizontalDistances.Length; ++i)
                    {
                        if (horizontalDistances[i].Distance >= 0.001f)
                        {
                            //Debug.Log(horizontalDistances[i].Distance);
                            continue;
                        }

                        horizontalVelocity += (horizontalDistances[i].SurfaceNormal * -horizontalDistances[i].Distance);
                    }

                    horizontalDistances.Dispose();
                }
            }

            horizontalCollisions.Dispose();
        }

        // Handles vertical movement from gravity and jumping.
        private void HandleVerticalMovement(
            ref float3 verticalVelocity,
            ref Entity entity,
            ref float3 currPos,
            ref quaternion currRot,
            ref CharacterControllerComponent controller,
            ref PhysicsCollider collider,
            ref CollisionWorld collisionWorld)
        {
            controller.VerticalVelocity = verticalVelocity;

            if (MathUtils.IsZero(verticalVelocity))
            {
                return;
            }

            verticalVelocity *= DeltaTime;

            NativeList<ColliderCastHit> verticalCollisions = PhysicsUtils.ColliderCastAll(collider, currPos, currPos + verticalVelocity, ref collisionWorld, entity, Allocator.Temp);
            PhysicsUtils.TrimByFilter(ref verticalCollisions, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);

            if (verticalCollisions.Length > 0)
            {
                RigidTransform transform = new RigidTransform()
                {
                    pos = currPos + verticalVelocity,
                    rot = currRot
                };

                if (PhysicsUtils.ColliderDistance(out DistanceHit verticalPenetration, collider, 1.0f, transform, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp))
                {
                    if (verticalPenetration.Distance < -0.01f)
                    {
                        verticalVelocity += (verticalPenetration.SurfaceNormal * verticalPenetration.Distance);

                        if (PhysicsUtils.ColliderCast(out ColliderCastHit adjustedHit, collider, currPos, currPos + verticalVelocity, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp))
                        {
                            verticalVelocity *= adjustedHit.Fraction;
                        }
                    }
                }
            }

            verticalVelocity = MathUtils.ZeroOut(verticalVelocity, 0.0001f);
            verticalCollisions.Dispose();
        }

        // Determines if the character is resting on a surface.
        private unsafe static void DetermineIfGrounded(Entity entity, ref float3 currPos, ref float3 epsilon, ref CharacterControllerComponent controller, ref PhysicsCollider collider, ref CollisionWorld collisionWorld)
        {
            var aabb = collider.ColliderPtr->CalculateAabb();
            float mod = 0.15f;

            float3 samplePos = currPos + new float3(0.0f, aabb.Min.y, 0.0f);
            float3 gravity = math.normalize(controller.Gravity);
            float3 offset = (gravity * 0.01f);

            float3 posLeft = samplePos - new float3(aabb.Extents.x * mod, 0.0f, 0.0f);
            float3 posRight = samplePos + new float3(aabb.Extents.x * mod, 0.0f, 0.0f);
            float3 posForward = samplePos + new float3(0.0f, 0.0f, aabb.Extents.z * mod);
            float3 posBackward = samplePos - new float3(0.0f, 0.0f, aabb.Extents.z * mod);

            controller.IsGrounded = PhysicsUtils.Raycast(out Unity.Physics.RaycastHit centerHit, samplePos, samplePos + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                    PhysicsUtils.Raycast(out Unity.Physics.RaycastHit leftHit, posLeft, posLeft + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                    PhysicsUtils.Raycast(out Unity.Physics.RaycastHit rightHit, posRight, posRight + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                    PhysicsUtils.Raycast(out Unity.Physics.RaycastHit forwardHit, posForward, posForward + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                    PhysicsUtils.Raycast(out Unity.Physics.RaycastHit backwardHit, posBackward, posBackward + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp);

        }
    }
}
