using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
[Serializable]
public struct BulletAgeComponent : IComponentData
{
    public BulletAgeComponent(float maxAge)
    {
        this.maxAge = maxAge;
        age = 0;
    }

    public float age;
    public float maxAge;
}
