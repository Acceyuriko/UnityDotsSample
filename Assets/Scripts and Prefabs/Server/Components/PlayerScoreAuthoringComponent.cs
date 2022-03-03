using System;
using Unity.Entities;

[GenerateAuthoringComponent]
[Serializable]
public struct PlayerScoreAuthoringComponent : IComponentData
{
    public Entity Prefab;
}
