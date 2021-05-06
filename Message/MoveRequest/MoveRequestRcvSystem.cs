using Unity.Entities;
using Unity.Transforms;
using DOTSNET;
using UnityEngine;

[DisableAutoCreation]
public class MoveRequestRcvSystem : NetworkServerMessageSystem<MoveRequestMessage>
{
    protected override void OnUpdate() {}
    protected override bool RequiresAuthentication() { return true; }
    protected override void OnMessage(int connectionId, MoveRequestMessage message)
    {
        if (server.spawned.TryGetValue(message.netId, out Entity entity))
        {
            if (connectionId == GetComponent<NetworkEntity>(entity).connectionId)
            {
                MoveRequestComponent com = new MoveRequestComponent
                {
                    moveForward = message.moveForward,
                    moveBackward = message.moveBackward,
                    moveLeft = message.moveRight,
                    moveRight = message.moveLeft,
                    jump = message.jump,
                    shift = message.shift,
                    cameraForward = message.cameraForward
                };

                SetComponent(entity, com);

                Debug.Log("Rcvd: MoveForward : " + GetComponent<MoveRequestComponent>(entity).moveForward.ToString());

                //put player logic call here to call it on the same tick ?
            }
        }
    }
}

