using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
[Serializable]
public struct PlayerAuthoringComponent : IComponentData
{
    public Entity Prefab;
}
