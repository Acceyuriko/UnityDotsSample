using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class PlayerDestructionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EndSimECB;

    protected override void OnCreate()
    {
        m_EndSimECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_EndSimECB.CreateCommandBuffer().AsParallelWriter();
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>();

        Entities
            .WithNativeDisableParallelForRestriction(commandTargetFromEntity)
            .WithAll<DestroyTag, PlayerTag>()
            .ForEach((Entity entity, int nativeThreadIndex, in PlayerEntityComponent playerEntity) =>
            {
                var state = commandTargetFromEntity[playerEntity.PlayerEntity];
                state.targetEntity = Entity.Null;
                commandTargetFromEntity[playerEntity.PlayerEntity] = state;

                commandBuffer.DestroyEntity(nativeThreadIndex, entity);
            }).WithBurst().ScheduleParallel();

        m_EndSimECB.AddJobHandleForProducer(Dependency);
    }
}
