using System.Diagnostics;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class AsteroidSpawnSystem : SystemBase
{
    private EntityQuery m_AsteroidQuery;

    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    private EntityQuery m_GameSettingsQuery;

    private Entity m_Prefab;

    private EntityQuery m_ConnectionGroup;

    protected override void OnCreate()
    {
        m_AsteroidQuery = GetEntityQuery(ComponentType.ReadWrite<AsteroidTag>());
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_GameSettingsQuery = GetEntityQuery(ComponentType.ReadWrite<GameSettingsComponent>());

        RequireForUpdate(m_GameSettingsQuery);

        m_ConnectionGroup = GetEntityQuery(ComponentType.ReadWrite<NetworkStreamConnection>());
    }

    protected override void OnUpdate()
    {
        if (m_ConnectionGroup.IsEmptyIgnoreFilter)
        {
            EntityManager.DestroyEntity(m_AsteroidQuery);
            return;
        }

        if (m_Prefab == Entity.Null)
        {
            m_Prefab = GetSingleton<AsteroidAuthoringComponent>().Prefab;
            return;
        }

        var settings = GetSingleton<GameSettingsComponent>();
        var commandBuffer = m_BeginSimECB.CreateCommandBuffer();
        var count = m_AsteroidQuery.CalculateEntityCountWithoutFiltering();
        var asteroidPrefab = m_Prefab;
        var rand = new Unity.Mathematics.Random((uint)Stopwatch.GetTimestamp());

        Job
            .WithCode(() =>
            {
                for (int i = count; i < settings.numAsteroids; ++i)
                {
                    var padding = 0.1f;

                    var xPosition = rand.NextFloat(-1f * (settings.levelWidth / 2 - padding), settings.levelWidth / 2 - padding);
                    var yPosition = rand.NextFloat(-1f * (settings.levelHeight / 2 - padding), settings.levelHeight / 2 - padding);
                    var zPosition = rand.NextFloat(-1f * (settings.levelDepth / 2 - padding), settings.levelDepth / 2 - padding);

                    var chooseFace = rand.NextFloat(0, 6);

                    if (chooseFace < 1) { xPosition = -1 * (settings.levelWidth / 2 - padding); }
                    else if (chooseFace < 2) { xPosition = settings.levelWidth / 2 - padding; }
                    else if (chooseFace < 3) { yPosition = -1 * (settings.levelHeight / 2 - padding); }
                    else if (chooseFace < 4) { yPosition = settings.levelHeight / 2 - padding; }
                    else if (chooseFace < 5) { zPosition = -1 * (settings.levelDepth / 2 - padding); }
                    else if (chooseFace < 6) { zPosition = settings.levelDepth / 2 - padding; }

                    var pos = new Translation { Value = new float3(xPosition, yPosition, zPosition) };
                    var e = commandBuffer.Instantiate(asteroidPrefab);
                    commandBuffer.SetComponent(e, pos);

                    var randomVel = new Vector3(rand.NextFloat(-1f, 1f), rand.NextFloat(-1f, 1f), rand.NextFloat(-1f, 1f));
                    randomVel.Normalize();
                    randomVel = randomVel * settings.asteroidVelocity;
                    var vel = new PhysicsVelocity { Linear = new float3(randomVel.x, randomVel.y, randomVel.z) };
                    commandBuffer.SetComponent(e, vel);
                }
            })
            .Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}