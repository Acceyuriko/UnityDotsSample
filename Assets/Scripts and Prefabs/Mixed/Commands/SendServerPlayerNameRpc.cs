using Unity.NetCode;
using Unity.Collections;

public struct SendServerPlayerNameRpc : IRpcCommand
{
    public FixedString64 playerName;
}
