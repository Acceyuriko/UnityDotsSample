using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct PlayerSpawningStateComponent : IComponentData
{
    public int IsSpawning;
}
