using Unity.NetCode;
using Unity.Collections;

public struct SendClientGameRpc : IRpcCommand
{
    public int levelWidth;
    public int levelHeight;
    public int levelDepth;
    public float playerForce;
    public float bulletVelocity;
    public FixedString64 gameName;
}
