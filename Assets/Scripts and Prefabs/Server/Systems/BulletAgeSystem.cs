using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class BulletAgeSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    private GhostPredictionSystemGroup m_PredictionGroup;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_PredictionGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();
        var deltaTime = m_PredictionGroup.Time.DeltaTime;

        Entities.ForEach((Entity entity, int nativeThreadIndex, ref BulletAgeComponent age) =>
        {
            age.age += deltaTime;
            if (age.age > age.maxAge)
            {
                commandBuffer.DestroyEntity(nativeThreadIndex, entity);
            }
        }).ScheduleParallel();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
