using System;
using Unity.Collections;
using Unity.Entities;

[Serializable]
public struct ServerDataComponent : IComponentData
{
    public FixedString64 GameName;
    public ushort GamePort;
}
