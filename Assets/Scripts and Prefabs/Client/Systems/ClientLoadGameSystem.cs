using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
[UpdateBefore(typeof(RpcSystem))]
public class ClientLoadGameSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        RequireForUpdate(GetEntityQuery(
            ComponentType.ReadOnly<SendClientGameRpc>(),
            ComponentType.ReadOnly<ReceiveRpcCommandRequestComponent>()
        ));

        RequireSingletonForUpdate<GameSettingsComponent>();
        RequireSingletonForUpdate<ClientDataComponent>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_BeginSimECB.CreateCommandBuffer();
        var rpcFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>();
        var gameSettingsEntity = GetSingletonEntity<GameSettingsComponent>();
        var getGameSettingsComponentData = GetComponentDataFromEntity<GameSettingsComponent>();
        var networkId = GetSingleton<NetworkIdComponent>();

        Entities
            .ForEach((Entity entity, in SendClientGameRpc request, in ReceiveRpcCommandRequestComponent requestSource) =>
            {
                commandBuffer.DestroyEntity(entity);

                if (!rpcFromEntity.HasComponent(requestSource.SourceConnection))
                {
                    return;
                }

                getGameSettingsComponentData[gameSettingsEntity] = new GameSettingsComponent
                {
                    levelWidth = request.levelWidth,
                    levelHeight = request.levelHeight,
                    levelDepth = request.levelDepth,
                    playerForce = request.playerForce,
                    bulletVelocity = request.bulletVelocity,
                };

                var gameNameEntity = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(gameNameEntity, new GameNameComponent
                {
                    GameName = request.gameName
                });

                commandBuffer.AddComponent(requestSource.SourceConnection, new PlayerSpawningStateComponent());
                commandBuffer.AddComponent(requestSource.SourceConnection, default(NetworkStreamInGame));

                var levelReq = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(levelReq, new SendServerGameLoadedRpc());
                commandBuffer.AddComponent(levelReq, new SendRpcCommandRequestComponent());

                var playerReq = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(playerReq, new SendServerPlayerNameRpc{});
                commandBuffer.AddComponent(playerReq, new SendRpcCommandRequestComponent());

                Debug.Log($"Client {networkId.Value} loaded game");
            })
            .Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
