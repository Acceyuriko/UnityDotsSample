using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct GameNameComponent : IComponentData
{
    public FixedString64 GameName;
}
