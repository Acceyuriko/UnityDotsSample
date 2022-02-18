using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ServerDataComponent : IComponentData
{
    public ushort GamePort;
}
