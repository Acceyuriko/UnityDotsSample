using System;
using Unity.Entities;

[GenerateAuthoringComponent]
[Serializable]
public struct PlayerEntityComponent : IComponentData
{
    public Entity PlayerEntity;
}
