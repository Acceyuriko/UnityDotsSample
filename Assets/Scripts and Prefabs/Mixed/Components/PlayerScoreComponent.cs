using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[GenerateAuthoringComponent]
[Serializable]
public struct PlayerScoreComponent : IComponentData
{
    [GhostField]
    public int networkId;
    [GhostField]
    public FixedString64 playerName;
    [GhostField]
    public int currentScore;
    [GhostField]
    public int highScore;
}
