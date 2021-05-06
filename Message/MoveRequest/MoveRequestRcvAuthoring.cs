using DOTSNET;
using UnityEngine;
using System;

public class MoveRequestRcvAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(MoveRequestRcvSystem);
}