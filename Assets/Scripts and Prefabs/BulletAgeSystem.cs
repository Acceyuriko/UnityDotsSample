using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class BulletAgeSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();
        var deltaTime = Time.DeltaTime;

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
