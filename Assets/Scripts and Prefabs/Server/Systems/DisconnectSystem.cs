using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;

[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class DisconnectSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem m_EndFixedStepSimECB;
    private EntityQuery m_DisconnectedNCEQuery;

    protected override void OnCreate()
    {
        m_EndFixedStepSimECB = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_DisconnectedNCEQuery = GetEntityQuery(ComponentType.ReadWrite<NetworkStreamDisconnected>());

        RequireForUpdate(m_DisconnectedNCEQuery);
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_EndFixedStepSimECB.CreateCommandBuffer();

        JobHandle disconnectNCEsDep;
        var disconnectedNCEsNative = m_DisconnectedNCEQuery.ToEntityArrayAsync(Allocator.TempJob, out disconnectNCEsDep);

        var getNetworkIdComponentData = GetComponentDataFromEntity<NetworkIdComponent>();

        Dependency = Entities
            .WithReadOnly(disconnectedNCEsNative)
            .WithDisposeOnCompletion(disconnectedNCEsNative)
            .WithAll<PlayerTag>()
            .ForEach((Entity entity, in GhostOwnerComponent ghostOwner) =>
            {
                for (int i = 0; i < disconnectedNCEsNative.Length; i++)
                {
                    if (getNetworkIdComponentData[disconnectedNCEsNative[i]].Value == ghostOwner.NetworkId)
                    {
                        commandBuffer.AddComponent<DestroyTag>(entity);
                    }
                }
            })
            .Schedule(JobHandle.CombineDependencies(Dependency, disconnectNCEsDep));

        m_EndFixedStepSimECB.AddJobHandleForProducer(Dependency);
    }
}
