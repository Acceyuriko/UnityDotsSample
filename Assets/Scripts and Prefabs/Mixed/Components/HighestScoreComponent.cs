using System;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;

[GenerateAuthoringComponent]
[Serializable]
public struct HighestScoreComponent : IComponentData
{
    [GhostField]
    public FixedString64 playerName;
    [GhostField]
    public int highestScore;
}
