using System;
using Unity.Collections;
using Unity.Entities;

[Serializable]
public struct ClientDataComponent : IComponentData
{
    public FixedString64 ConnectToServerIp;
    public ushort GamePort;
}
