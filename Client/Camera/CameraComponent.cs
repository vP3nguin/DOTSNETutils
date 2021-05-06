using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Assigned to the Entity which the Camera should focus on.
public struct CameraComponent : IComponentData
{
    private float _pitch;
    private float _zoom;

    // The Yaw angle of the camera. For the standard camera this determines how it is rotated around the followed entity.
    public float Yaw { get; set; }
    public float YawSpeed { get; set; }

    // The Pitch angle of the camera. For the standard camera this determines how high it is above the followed entity.
    public float Pitch
    {
        get { return _pitch; }
        set { _pitch = math.clamp(value, MinPitch, MaxPitch); }
    }
    public float PitchSpeed { get; set; }

    // Minimum allowed Pitch angle. Used to clamp the camera and prevents it from being able to see straight horizontally or even up.
    public float MinPitch { get; set; }

    // Maximum allowed Pitch angle. Used to clamp the camera and prevent it from being able to look straight down which ruins certain effects (such as grass billboards).
    public float MaxPitch { get; set; }

    // The Zoom of the camera, which determines how far away from the followed entity it is.
    public float Zoom
    {
        get { return _zoom; }
        set { _zoom = math.clamp(value, MinZoom, MaxZoom); }
    }
    public float ZoomSpeed { get; set; }

    // The minimum Zoom of the camera. Used to clamp the camera and prevent it from being moved too near/into the followed entity
    public float MinZoom { get; set; }

    // The maximum Zoom of the camera. Used to clamp the camera and prevent it from being moved too far from the entity which may reveal too much of the scene.
    public float MaxZoom { get; set; }

    public float CameraYOffset { get; set; }

    public bool InvertYAxis { get; set; }

    public bool UseLerp { get; set; }

    public float LerpSpeed { get; set; }

    // The normalized camera forward vector.
    public Vector3 Forward { get; set; }

    // The normalize camera right vector.
    public Vector3 Right { get; set; }
}