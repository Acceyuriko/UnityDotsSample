using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerCommand : ICommandData
{
    public uint Tick { get; set; }
    public byte right;
    public byte left;
    public byte thrust;
    public byte reverseThrust;
    public byte selfDestruct;
    public byte shoot;
    public float mouseX;
    public float mouseY;
}