using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;


[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
[UpdateAfter(typeof(GhostSpawnClassificationSystem))]
public class PlayerGhostSpawnClassificationSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    private Entity m_CameraPrefab;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();

        RequireSingletonForUpdate<NetworkIdComponent>();
        RequireSingletonForUpdate<CameraAuthoringComponent>();
    }

    struct GhostPlayerState : ISystemStateComponentData { }

    protected override void OnUpdate()
    {
        if (m_CameraPrefab == Entity.Null)
        {
            m_CameraPrefab = GetSingleton<CameraAuthoringComponent>().Prefab;
            return;
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();
        var camera = m_CameraPrefab;
        var playerEntity = GetSingletonEntity<NetworkIdComponent>();
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>(false);

        Entities
            .WithAll<PlayerTag, PredictedGhostComponent>()
            .WithNone<GhostPlayerState>()
            .WithNativeDisableParallelForRestriction(commandTargetFromEntity)
            .ForEach((Entity entity, int entityInQueryIndex) =>
            {
                var state = commandTargetFromEntity[playerEntity];
                state.targetEntity = entity;
                commandTargetFromEntity[playerEntity] = state;

                commandBuffer.AddComponent(entityInQueryIndex, entity, new GhostPlayerState())  ;

                var cameraEntity = commandBuffer.Instantiate(entityInQueryIndex, camera);
                commandBuffer.AddComponent(entityInQueryIndex, cameraEntity, new Parent { Value = entity });
                commandBuffer.AddComponent(entityInQueryIndex, cameraEntity, new LocalToParent());
            })
            .ScheduleParallel();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
