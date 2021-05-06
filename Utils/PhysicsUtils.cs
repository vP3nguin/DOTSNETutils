using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

// Collection of utility physics operations.
public static class PhysicsUtils
{
    // Returns a list of all colliders within the specified distance of the provided collider, as a list of <see cref="DistanceHit"/>.<para/>
    // Can be used in conjunction with <see cref="ColliderCastAll"/> to get the penetration depths of any colliders (using the distance of collisions).<para/>
    // The caller must dispose of the returned list.
    public unsafe static NativeList<DistanceHit> ColliderDistanceAll(PhysicsCollider collider, float maxDistance, RigidTransform transform, ref CollisionWorld collisionWorld, Entity ignore, Allocator allocator = Allocator.TempJob)
    {
        ColliderDistanceInput input = new ColliderDistanceInput()
        {
            Collider = collider.ColliderPtr,
            MaxDistance = maxDistance,
            Transform = transform
        };

        NativeList<DistanceHit> allDistances = new NativeList<DistanceHit>(allocator);

        if (collisionWorld.CalculateDistance(input, ref allDistances))
        {
            TrimByEntity(ref allDistances, ignore);
        }

        return allDistances;
    }

    // Performs a collider cast using the specified collider.
    public static bool ColliderDistance(
        out DistanceHit smallestDistanceHit,
        PhysicsCollider collider,
        float maxDistance,
        RigidTransform transform,
        ref CollisionWorld collisionWorld,
        Entity ignore,
        CollisionFilter? filter = null,
        EntityManager? manager = null,
        ComponentDataFromEntity<PhysicsCollider>? colliderData = null,
        Allocator allocator = Allocator.TempJob)
    {
        var allDistances = ColliderDistanceAll(collider, maxDistance, transform, ref collisionWorld, ignore, allocator);

        if (filter.HasValue)
        {
            if (manager.HasValue)
            {
                TrimByFilter(ref allDistances, manager.Value, filter.Value);
            }
            else if (colliderData.HasValue)
            {
                TrimByFilter(ref allDistances, colliderData.Value, filter.Value);
            }
        }

        GetSmallestFractional(ref allDistances, out smallestDistanceHit);
        allDistances.Dispose();

        return true;
    }

    // Performs a collider cast along the specified ray and returns all resulting <see cref="ColliderCastHit"/>s.<para/>
    // The caller must dispose of the returned list.
    public unsafe static NativeList<ColliderCastHit> ColliderCastAll(PhysicsCollider collider, float3 from, float3 to, ref CollisionWorld collisionWorld, Entity ignore, Allocator allocator = Allocator.TempJob)
    {
        ColliderCastInput input = new ColliderCastInput()
        {
            Collider = collider.ColliderPtr,
            Start = from,
            End = to
        };

        NativeList<ColliderCastHit> allHits = new NativeList<ColliderCastHit>(allocator);

        if (collisionWorld.CastCollider(input, ref allHits))
        {
            TrimByEntity(ref allHits, ignore);
        }

        return allHits;
    }

    // Performs a collider cast along the specified ray.<para/>
    // Will return true if there was a collision and populate the provided <see cref="ColliderCastHit"/>.
    public unsafe static bool ColliderCast(
        out ColliderCastHit nearestHit,
        PhysicsCollider collider,
        float3 from,
        float3 to,
        ref CollisionWorld collisionWorld,
        Entity ignore,
        CollisionFilter? filter = null,
        EntityManager? manager = null,
        ComponentDataFromEntity<PhysicsCollider>? colliderData = null,
        Allocator allocator = Allocator.TempJob)
    {
        nearestHit = new ColliderCastHit();
        NativeList<ColliderCastHit> allHits = ColliderCastAll(collider, from, to, ref collisionWorld, ignore, allocator);

        if (filter.HasValue)
        {
            if (manager.HasValue)
            {
                TrimByFilter(ref allHits, manager.Value, filter.Value);
            }
            else if (colliderData.HasValue)
            {
                TrimByFilter(ref allHits, colliderData.Value, filter.Value);
            }
        }

        GetSmallestFractional(ref allHits, out nearestHit);
        allHits.Dispose();

        return true;
    }

    // Performs a raycast along the specified ray and returns all resulting <see cref="RaycastHit"/>s.<para/>
    // The caller must dispose of the returned list.
    public unsafe static NativeList<RaycastHit> RaycastAll(float3 from, float3 to, ref CollisionWorld collisionWorld, Entity ignore, CollisionFilter? filter = null, Allocator allocator = Allocator.TempJob)
    {
        RaycastInput input = new RaycastInput()
        {
            Start = from,
            End = to,
            Filter = (filter.HasValue ? filter.Value : PhysicsCollisionFilters.AllWithAll)
        };

        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(allocator);

        if (collisionWorld.CastRay(input, ref allHits))
        {
            TrimByEntity(ref allHits, ignore);
        }

        return allHits;
    }

    // Performs a raycast along the specified ray.<para/>
    // Will return true if there was a collision and populate the provided <see cref="RaycastHit"/>.
    public unsafe static bool Raycast(out RaycastHit nearestHit, float3 from, float3 to, ref CollisionWorld collisionWorld, Entity ignore, CollisionFilter? filter = null, Allocator allocator = Allocator.TempJob)
    {
        NativeList<RaycastHit> allHits = RaycastAll(from, to, ref collisionWorld, ignore, filter, allocator);

        bool gotHit = GetSmallestFractional(ref allHits, out nearestHit);
        allHits.Dispose();

        return gotHit;
    }

    // Given a list of <see cref="IQueryResult"/> objects (ie from <see cref="ColliderCastAll"/> or <see cref="ColliderDistanceAll"/>),
    // removes any entities that:
    public unsafe static void TrimByFilter<T>(ref NativeList<T> castResults, EntityManager entityManager, CollisionFilter filter) where T : struct, IQueryResult
    {
        for (int i = (castResults.Length - 1); i >= 0; --i)
        {
            if (entityManager.HasComponent<PhysicsCollider>(castResults[i].Entity))
            {
                PhysicsCollider collider = entityManager.GetComponentData<PhysicsCollider>(castResults[i].Entity);

                if (CollisionFilter.IsCollisionEnabled(filter, collider.ColliderPtr->Filter))
                {
                    continue;
                }
            }

            castResults.RemoveAt(i);
        }
    }

    // Variant of <see cref="TrimByFilter{T}(ref NativeList{T}, EntityManager, CollisionFilter)"/> to be used within a Job which does not have access to an <see cref="EntityManager"/>.
    public unsafe static void TrimByFilter<T>(ref NativeList<T> castResults, ComponentDataFromEntity<PhysicsCollider> colliderData, CollisionFilter filter) where T : struct, IQueryResult
    {
        for (int i = (castResults.Length - 1); i >= 0; --i)
        {
            if (colliderData.HasComponent(castResults[i].Entity))
            {
                PhysicsCollider collider = colliderData[castResults[i].Entity];

                if (CollisionFilter.IsCollisionEnabled(filter, collider.ColliderPtr->Filter))
                {
                    continue;
                }
            }

            castResults.RemoveAt(i);
        }
    }

    // The specified entity is removed from the provided list if it is present.
    public static void TrimByEntity<T>(ref NativeList<T> castResults, Entity ignore) where T : struct, IQueryResult
    {
        if (ignore == Entity.Null)
        {
            return;
        }

        for (int i = (castResults.Length - 1); i >= 0; --i)
        {
            if (ignore == castResults[i].Entity)
            {
                castResults.RemoveAt(i);
            }
        }
    }

    // Retrieves the smallest <see cref="IQueryResult.Fraction"/> result in the provided list.
    public static bool GetSmallestFractional<T>(ref NativeList<T> castResults, out T nearest) where T : struct, IQueryResult
    {
        nearest = default(T);

        if (castResults.Length == 0)
        {
            return false;
        }

        float smallest = float.MaxValue;

        for (int i = 0; i < castResults.Length; ++i)
        {
            if (castResults[i].Fraction < smallest)
            {
                smallest = castResults[i].Fraction;
                nearest = castResults[i];
            }
        }

        return true;
    }
}
