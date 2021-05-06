using Unity.Entities;
using UnityEngine;
using DOTSNET;
using System;
using Unity.Transforms;
using Unity.Mathematics;

[DisallowMultipleComponent]
public class PlayerControllerSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(PlayerControllerSystem);
}

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
[DisableAutoCreation]
// Main control system for player input.
public class PlayerControllerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<PlayerComponent>()
            .ForEach((
            Entity entity,
            ref NetworkEntity netentity,
            ref Rotation rotation,
            ref CameraComponent camera,
            ref CharacterControllerComponent controller) =>
        {
            if (netentity.owned)
            {
                ProcessMovement(ref controller, ref camera);
                ProcessJump(ref controller);
                ProcessRotation(ref controller, ref rotation);
            }
        });
    }

    // Processes the horizontal movement input from the player to move the entity along the xz plane.
    private void ProcessMovement(ref CharacterControllerComponent controller, ref CameraComponent camera)
    {
        float movementX = (Input.GetAxis("Move Right") > 0.0f ? 1.0f : 0.0f) + (Input.GetAxis("Move Left") > 0.0f ? -1.0f : 0.0f);
        float movementZ = (Input.GetAxis("Move Forward") > 0.0f ? 1.0f : 0.0f) + (Input.GetAxis("Move Backward") > 0.0f ? -1.0f : 0.0f);

        float magnitude = 1.0f;

        Vector3 forward = new Vector3(camera.Forward.x, 0.0f, camera.Forward.z).normalized;
        Vector3 right = new Vector3(camera.Right.x, 0.0f, camera.Right.z).normalized;

        if (!MathUtils.IsZero(movementX) || !MathUtils.IsZero(movementZ))
        {
            controller.CurrentDirection = ((forward * movementZ) + (right * movementX)).normalized;

            if (Input.GetKey(KeyCode.LeftShift)) { magnitude *= controller.RunFactor; }
            if (!controller.IsGrounded) { magnitude *= controller.InAirFactor; }
        }
        else
        {
            magnitude = 0.0f;
        }

        controller.CurrentMagnitude = magnitude;
    }

    private void ProcessJump(ref CharacterControllerComponent controller)
    {
        controller.Jump = Input.GetAxis("Jump") > 0.0f;
    }
    
    private void ProcessRotation(ref CharacterControllerComponent controller, ref Rotation rotation)
    {
        if (Input.GetAxis("Move Forward") != 0.0f || Input.GetAxis("Move Backward") != 0.0f || 
            Input.GetAxis("Move Right") != 0.0f || Input.GetAxis("Move Left") != 0.0f)
        {
            quaternion newRotation = Quaternion.Lerp(rotation.Value, quaternion.LookRotation(controller.CurrentDirection, new float3(0, 1, 0)), controller.RotationLerpSpeed);
            
            rotation.Value = newRotation;
        }
    }
}
