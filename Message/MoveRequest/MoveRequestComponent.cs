using System;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

// Indicates that the entity is controlled directly by player input.
public struct MoveRequestComponent : IComponentData
{
    public bool moveForward;
    public bool moveBackward;
    public bool moveRight;
    public bool moveLeft;
    public bool jump;
    public bool shift;

    public float3 cameraForward;
}
