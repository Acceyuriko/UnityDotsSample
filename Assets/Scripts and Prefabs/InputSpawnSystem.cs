using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class InputSpawnSystem : SystemBase
{
    private EntityQuery m_PlayerQuery;

    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    private Entity m_PlayerPrefab;

    private Entity m_BulletPrefab;

    private float m_PerSecond = 10f;
    private float m_NextTime = 0;

    protected override void OnCreate()
    {
        m_PlayerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>());
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        RequireSingletonForUpdate<PlayerAuthoringComponent>();
    }

    protected override void OnUpdate()
    {
        if (m_PlayerPrefab == Entity.Null || m_BulletPrefab == Entity.Null)
        {
            m_PlayerPrefab = GetSingleton<PlayerAuthoringComponent>().Prefab;
            m_BulletPrefab = GetSingleton<BulletAuthoringComponent>().Prefab;
            return;
        }
        byte shoot, selfDestruct;
        shoot = selfDestruct = 0;
        var playerCount = m_PlayerQuery.CalculateEntityCountWithoutFiltering();

        if (Input.GetKey("space"))
        {
            shoot = 1;
        }
        if (Input.GetKey("p"))
        {
            selfDestruct = 1;
        }
        if (shoot == 1 && playerCount < 1)
        {
            EntityManager.Instantiate(m_PlayerPrefab);
            return;
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();

        var gameSettings = GetSingleton<GameSettingsComponent>();
        var bulletPrefab = m_BulletPrefab;

        var canShoot = false;
        if (UnityEngine.Time.time >= m_NextTime)
        {
            canShoot = true;
            m_NextTime += (1 / m_PerSecond);
        }

        Entities
            .WithAll<PlayerTag>()
            .ForEach((Entity entity, int nativeThreadIndex, in Translation position, in Rotation rotation, in VelocityComponent velocity, in BulletSpawnOffsetComponent bulletOffset) =>
            {
                if (selfDestruct == 1)
                {
                    commandBuffer.AddComponent(nativeThreadIndex, entity, new DestroyTag());
                }
                if (shoot != 1 || !canShoot)
                {
                    return;
                }
                var bulletEntity = commandBuffer.Instantiate(nativeThreadIndex, bulletPrefab);

                var newPosition = new Translation { Value = position.Value + math.mul(rotation.Value, bulletOffset.Value).xyz };
                commandBuffer.SetComponent(nativeThreadIndex, bulletEntity, newPosition);

                var vel = new VelocityComponent { Value = gameSettings.bulletVelocity * math.mul(rotation.Value, new float3(0, 0, 1).xyz) + velocity.Value };
                commandBuffer.SetComponent(nativeThreadIndex, bulletEntity, vel);
            })
            .ScheduleParallel();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
