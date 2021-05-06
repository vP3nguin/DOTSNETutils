using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using DOTSNET;
using System;

[DisallowMultipleComponent]
public class CameraSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(CameraSystem);
}

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
[DisableAutoCreation]
// Basic system which follows the entity with the <see cref="CameraFollowComponent"/>.
public sealed class CameraSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities
            .ForEach((
            Entity entity,
            ref NetworkEntity netentity,
            ref Translation position,
            ref Rotation rotation,
            ref CameraComponent camera) =>
        {
            if (netentity.owned)
            {
                // Process input
                ProcessCameraInput(ref camera);

                // Target to look at
                Vector3 target = position.Value + new float3(0, camera.CameraYOffset, 0);

                // Create default offsets
                float3 newPosition = new float3(position.Value.x, position.Value.y + camera.CameraYOffset, position.Value.z - camera.Zoom);
                Camera.main.transform.position = newPosition;

                // Rotate around player
                Camera.main.transform.RotateAround(target, Vector3.up, camera.Yaw);
                Vector3 direction = (target - Camera.main.transform.position).normalized;
                float3 right = new float3(direction.z, direction.y, -direction.x);
                Camera.main.transform.RotateAround(target, right, camera.Pitch);

                // Apply look at
                Camera.main.transform.LookAt(target);

                // Set forward/right vector reference for movement
                camera.Forward = Camera.main.transform.forward;
                camera.Right = Camera.main.transform.right;
            }
        });
    }

    // Handles all camera related input.
    private bool ProcessCameraInput(ref CameraComponent camera)
    {
        return ProcessCameraZoom(ref camera) ||
                ProcessCameraYawPitch(ref camera);
    }

    // Handles input for zooming the camera in and out.
    private bool ProcessCameraZoom(ref CameraComponent camera)
    {
        float scroll = Input.GetAxis("Mouse Scroll Wheel");

        if (!MathUtils.IsZero(scroll))
        {
            camera.Zoom -= scroll * camera.ZoomSpeed;
            return true;
        }

        return false;
    }

    // Handles input for manipulating the camera yaw (rotating around).
    private bool ProcessCameraYawPitch(ref CameraComponent camera)
    {
        if (MathUtils.IsZero(Input.GetAxis("Mouse Right")))
        {
            return false;
        }

        camera.Yaw += Input.GetAxis("Mouse X") * camera.YawSpeed;

        if (camera.InvertYAxis)
        {
            camera.Pitch += Input.GetAxis("Mouse Y") * camera.PitchSpeed;
        }
        else
        {
            camera.Pitch -= Input.GetAxis("Mouse Y") * camera.PitchSpeed;
        }

        return true;
    }
}
