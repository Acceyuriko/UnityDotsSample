using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class AsteroidsDestructionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EndSimECB;

    protected override void OnCreate()
    {
        m_EndSimECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_EndSimECB.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithAll<DestroyTag, AsteroidTag>()
            .ForEach((Entity entity, int nativeThreadIndex) =>
            {
                commandBuffer.DestroyEntity(nativeThreadIndex, entity);
            }).ScheduleParallel();

        m_EndSimECB.AddJobHandleForProducer(Dependency);
    }
}
