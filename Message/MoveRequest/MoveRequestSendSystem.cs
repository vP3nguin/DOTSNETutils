using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using DOTSNET;
using UnityEngine;

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
[DisableAutoCreation]
public class MoveRequestSendSystem : SystemBase
{
    [AutoAssign] protected NetworkClientSystem client;

    public float interval = 0.03f;
    double lastSendTime;

    NativeList<MoveRequestMessage> messages;

    protected override void OnCreate()
    {
        base.OnCreate();
        messages = new NativeList<MoveRequestMessage>(10, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (Time.ElapsedTime >= lastSendTime + interval)
        {
            bool moveForward = Input.GetAxis("Move Forward") > 0.0f;
            bool moveBackward = Input.GetAxis("Move Backward") > 0.0f;
            bool moveRight = Input.GetAxis("Move Right") > 0.0f;
            bool moveLeft = Input.GetAxis("Move Left") > 0.0f;
            bool jump = Input.GetAxis("Jump") > 0.0f;
            bool shift = Input.GetKey(KeyCode.LeftShift);
            float3 cameraForward = Camera.main.transform.forward;

            NativeList<MoveRequestMessage> _messages = messages;

            Entities
                .WithAll<PlayerComponent>()
                .ForEach((in NetworkEntity networkEntity, in MoveRequestComponent input) =>
            {
                if (!networkEntity.owned)
                    return;

                MoveRequestMessage message = new MoveRequestMessage(
                    networkEntity.netId,
                    moveForward,
                    moveBackward,
                    moveRight,
                    moveLeft,
                    jump,
                    shift,
                    cameraForward
                );

                _messages.Add(message);
            })
            .Run();

            client.Send(_messages);
            messages.Clear();

            lastSendTime = Time.ElapsedTime;
        }
    }
}

