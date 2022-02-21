using System.Diagnostics;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public struct PlayerSpawnInProgressTag : IComponentData { }

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class PlayerSpawnSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;
    private Entity m_Prefab;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        RequireSingletonForUpdate<GameSettingsComponent>();
    }

    protected override void OnUpdate()
    {
        if (m_Prefab == Entity.Null)
        {
            m_Prefab = GetSingleton<PlayerAuthoringComponent>().Prefab;
            return;
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer();
        var playerPrefab = m_Prefab;
        var rand = new Unity.Mathematics.Random((uint)Stopwatch.GetTimestamp());
        var gameSettings = GetSingleton<GameSettingsComponent>();

        var playerStateFromEntity = GetComponentDataFromEntity<PlayerSpawningStateComponent>();

        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>();
        var networkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>();

        Entities
            .ForEach((Entity entity, in PlayerSpawnRequestRpc request, in ReceiveRpcCommandRequestComponent requestSource) =>
            {
                commandBuffer.DestroyEntity(entity);

                if (
                    !playerStateFromEntity.HasComponent(requestSource.SourceConnection) ||
                    !commandTargetFromEntity.HasComponent(requestSource.SourceConnection) ||
                    commandTargetFromEntity[requestSource.SourceConnection].targetEntity != Entity.Null ||
                    playerStateFromEntity[requestSource.SourceConnection].IsSpawning != 0
                )
                {
                    return;
                }

                var player = commandBuffer.Instantiate(playerPrefab);

                var width = gameSettings.levelWidth * 2f;
                var height = gameSettings.levelHeight * 2f;
                var depth = gameSettings.levelDepth * 2f;

                var pos = new Translation
                {
                    Value = new float3(
                        rand.NextFloat(-width, width),
                        rand.NextFloat(-height, height),
                        rand.NextFloat(-depth, depth)
                    )
                };

                var rot = new Rotation { Value = Quaternion.identity };

                commandBuffer.SetComponent(player, pos);
                commandBuffer.SetComponent(player, rot);

                commandBuffer.SetComponent(player, new GhostOwnerComponent { NetworkId = networkIdFromEntity[requestSource.SourceConnection].Value });
                commandBuffer.SetComponent(player, new PlayerEntityComponent { PlayerEntity = requestSource.SourceConnection });

                commandBuffer.AddComponent(player, new PlayerSpawnInProgressTag());

                playerStateFromEntity[requestSource.SourceConnection] = new PlayerSpawningStateComponent { IsSpawning = 1 };
            })
            .Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSendSystem))]
public class PlayerCompleteSpawnSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_BeginSimECB.CreateCommandBuffer();

        var playerStateFromEntity = GetComponentDataFromEntity<PlayerSpawningStateComponent>();
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>();
        var connectionFromEntity = GetComponentDataFromEntity<NetworkStreamConnection>();

        Entities
            .WithAll<PlayerSpawnInProgressTag>()
            .ForEach((Entity entity, in PlayerEntityComponent player) =>
            {
                if (
                    !playerStateFromEntity.HasComponent(player.PlayerEntity) ||
                    !connectionFromEntity[player.PlayerEntity].Value.IsCreated
                )
                {
                    commandBuffer.DestroyEntity(entity);
                    return;
                }

                commandBuffer.RemoveComponent<PlayerSpawnInProgressTag>(entity);
                commandTargetFromEntity[player.PlayerEntity] = new CommandTargetComponent { targetEntity = entity };
                playerStateFromEntity[player.PlayerEntity] = new PlayerSpawningStateComponent { IsSpawning = 0 };
            })
            .Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}