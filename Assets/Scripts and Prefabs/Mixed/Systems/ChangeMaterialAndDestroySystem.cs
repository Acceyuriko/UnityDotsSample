using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(TriggerEventConversionSystem))]
public class ChangeMaterialAndDestroySystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem m_EndFixedStepSimECB;
    private TriggerEventConversionSystem m_TriggerEventConversionsystem;
    private EntityQueryMask m_NonTriggerMask;

    protected override void OnCreate()
    {
        m_EndFixedStepSimECB = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_TriggerEventConversionsystem = World.GetOrCreateSystem<TriggerEventConversionSystem>();
        m_NonTriggerMask = EntityManager.GetEntityQueryMask(
            GetEntityQuery(new EntityQueryDesc
            {
                None = new ComponentType[]
                {
                    typeof(StatefulTriggerEvent)
                }
            })
        );
    }

    protected override void OnUpdate()
    {
        Dependency = JobHandle.CombineDependencies(m_TriggerEventConversionsystem.OutDependency, Dependency);

        var commandBuffer = m_EndFixedStepSimECB.CreateCommandBuffer();
        var nonTriggerMask = m_NonTriggerMask;

        Entities
            .WithName("ChangeMaterialAndDdestroySystem")
            .WithoutBurst()
            .ForEach((Entity entity, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer) =>
            {
                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];
                    var otherEntity = triggerEvent.GetOtherEntity(entity);

                    if (triggerEvent.State == StatefulTriggerEvent.EventOverlapState.Stay || !nonTriggerMask.Matches(otherEntity))
                    {
                        continue;
                    }
                    if (triggerEvent.State == StatefulTriggerEvent.EventOverlapState.Enter)
                    {
                        var bulletRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(entity);
                        var asteroidRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(otherEntity);
                        asteroidRenderMesh.material = bulletRenderMesh.material;

                        commandBuffer.SetSharedComponent(otherEntity, asteroidRenderMesh);
                    }
                    else
                    {
                        commandBuffer.AddComponent(otherEntity, new DestroyTag { });
                    }
                }
            })
            .Run();

        m_EndFixedStepSimECB.AddJobHandleForProducer(Dependency);
    }
}
