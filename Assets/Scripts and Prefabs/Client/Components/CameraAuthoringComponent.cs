using System;
using Unity.Entities;

[GenerateAuthoringComponent]
[Serializable]
public struct CameraAuthoringComponent : IComponentData
{
    public Entity Prefab;
}
