using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct CharacterControllerComponent : IComponentData
{
/*--INPUT--*/

    // The current direction that the character is moving
    public float3 CurrentDirection { get; set; }

    // The current magnitude of the character movement.
    // If 0.0, then the character is not being directly moved by the controller but residual forces may still be active.
    public float3 CurrentMagnitude { get; set; }

    // Is the character requesting to jump?
    public bool Jump { get; set; }

    /*--CONTROL--*/

    // Gravity force applied to the character.
    public float3 Gravity { get; set; }

    // The maximum speed at which this character moves.
    public float MaxSpeed { get; set; }

    // The current speed at which the player moves.
    public float Speed { get; set; }

    public float RunFactor { get; set; }

    // The jump strength which controls how high a jump is.
    public float JumpStrength { get; set; }

    public float InAirFactor { get; set; }

    // The maximum height the character can step up, in world units.
    public float MaxStep { get; set; }

    // Drag value applied to reduce the <see cref="JumpVelocity"/>.
    public float Drag { get; set; }

    public float RotationLerpSpeed { get; set; }

    /*--INTERNAL STATE--*/

    // True if the character is on the ground.
    public bool IsGrounded { get; set; }

    // The current horizontal velocity of the character.
    public float3 HorizontalVelocity { get; set; }

    // The current jump velocity of the character.
    public float3 VerticalVelocity { get; set; }
}
