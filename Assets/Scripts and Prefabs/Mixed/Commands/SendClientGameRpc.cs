using Unity.NetCode;

public struct SendClientGameRpc : IRpcCommand
{
    public int levelWidth;
    public int levelHeight;
    public int levelDepth;
    public float playerForce;
    public float bulletVelocity;
}
