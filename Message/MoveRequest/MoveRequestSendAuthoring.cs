using DOTSNET;
using UnityEngine;
using System;

public class MoveRequestSendAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(MoveRequestSendSystem);
}