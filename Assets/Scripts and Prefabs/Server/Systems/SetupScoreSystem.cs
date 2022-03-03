using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class SetupScoreSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;
    private EntityQuery m_HighestScoreQuery;
    private EntityQuery m_PlayerScoresQuery;
    private Entity m_Prefab;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_HighestScoreQuery = GetEntityQuery(ComponentType.ReadOnly<HighestScoreComponent>());
        m_PlayerScoresQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerScoreComponent>());

        RequireForUpdate(GetEntityQuery(
            ComponentType.ReadOnly<SendServerPlayerNameRpc>(),
            ComponentType.ReadOnly<ReceiveRpcCommandRequestComponent>()
        ));
    }

    protected override void OnUpdate()
    {
        if (m_Prefab == Entity.Null)
        {
            m_Prefab = GetSingleton<PlayerScoreAuthoringComponent>().Prefab;
            var initialPlayerScore = EntityManager.Instantiate(m_Prefab);
            EntityManager.SetComponentData<PlayerScoreComponent>(
                initialPlayerScore,
                new PlayerScoreComponent
                {
                    networkId = 1,
                }
            );
            return;
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer();
        var rpcFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>();
        var currentPlayerScoreEntities = m_PlayerScoresQuery.ToEntityArray(Allocator.TempJob);
        var playerScoreComponent = GetComponentDataFromEntity<PlayerScoreComponent>();
        var scorePrefab = m_Prefab;
        var networkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>();

        Entities
            .WithDisposeOnCompletion(currentPlayerScoreEntities)
            .ForEach((Entity entity, in SendServerPlayerNameRpc request, in ReceiveRpcCommandRequestComponent requestSource) =>
            {
                commandBuffer.DestroyEntity(entity);

                if (!rpcFromEntity.HasComponent(requestSource.SourceConnection))
                {
                    return;
                }

                var newPlayersNetWorkId = networkIdFromEntity[requestSource.SourceConnection].Value;
                var newPlayerScore = new PlayerScoreComponent
                {
                    networkId = newPlayersNetWorkId,
                    playerName = request.playerName,
                    currentScore = 0,
                    highScore = 0
                };

                bool uniqueNetworkId = true;
                for (int i = 0; i < currentPlayerScoreEntities.Length; ++i)
                {
                    var componentData = playerScoreComponent[currentPlayerScoreEntities[i]];
                    if (componentData.networkId == newPlayersNetWorkId)
                    {
                        commandBuffer.SetComponent<PlayerScoreComponent>(currentPlayerScoreEntities[i], newPlayerScore);
                        uniqueNetworkId = false;
                    }
                }
                if (uniqueNetworkId)
                {
                    var playerScoreEntity = commandBuffer.Instantiate(scorePrefab);
                    commandBuffer.SetComponent<PlayerScoreComponent>(playerScoreEntity, newPlayerScore);
                }
            })
            .Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
