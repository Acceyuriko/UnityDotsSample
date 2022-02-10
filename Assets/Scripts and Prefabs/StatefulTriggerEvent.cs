using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

[Serializable]
public struct StatefulTriggerEvent : IBufferElementData, IComparable<StatefulTriggerEvent>
{
    public enum EventOverlapState : byte
    {
        Enter,
        Stay,
        Exit,
    }

    public EntityPair Entities;
    public BodyIndexPair BodyIndices;
    public ColliderKeyPair ColliderKeys;
    public EventOverlapState State;

    public StatefulTriggerEvent(
        Entity entityA,
        Entity entityB,
        int bodyIndexA,
        int bodyIndexB,
        ColliderKey colliderKeyA,
        ColliderKey colliderKeyB
    )
    {
        Entities = new EntityPair
        {
            EntityA = entityA,
            EntityB = entityB
        };
        BodyIndices = new BodyIndexPair
        {
            BodyIndexA = bodyIndexA,
            BodyIndexB = bodyIndexB,
        };
        ColliderKeys = new ColliderKeyPair
        {
            ColliderKeyA = colliderKeyA,
            ColliderKeyB = colliderKeyB,
        };
        State = default;
    }

    public Entity GetOtherEntity(Entity entity)
    {
        if (entity == Entities.EntityA)
        {
            return Entities.EntityB;
        }
        return Entities.EntityA;
    }

    public int CompareTo(StatefulTriggerEvent other)
    {
        var result = Entities.EntityA.CompareTo(other.Entities.EntityA);
        if (result != 0)
        {
            return result;
        }
        result = Entities.EntityB.CompareTo(other.Entities.EntityB);
        if (result != 0)
        {
            return result;
        }
        if (ColliderKeys.ColliderKeyA.Value != other.ColliderKeys.ColliderKeyA.Value)
        {
            return ColliderKeys.ColliderKeyA.Value < other.ColliderKeys.ColliderKeyA.Value ? -1 : 1;
        }
        if (ColliderKeys.ColliderKeyB.Value != other.ColliderKeys.ColliderKeyB.Value)
        {
            return ColliderKeys.ColliderKeyB.Value < other.ColliderKeys.ColliderKeyB.Value ? -1 : 1;
        }
        return 0;
    }
}
