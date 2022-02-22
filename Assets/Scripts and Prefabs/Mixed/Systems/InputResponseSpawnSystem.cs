using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Networking.Transport.Utilities;
using Unity.Physics;

[UpdateInWorld(UpdateInWorld.TargetWorld.ClientAndServer)]
public class InputResponseSpawnSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;
    private GhostPredictionSystemGroup m_PredictionGroup;
    private Entity m_BulletPrefab;

    private const int k_CoolDownTicksCount = 5;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_PredictionGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();

        RequireSingletonForUpdate<GameSettingsComponent>();
    }

    protected override void OnUpdate()
    {
        if (m_BulletPrefab == Entity.Null)
        {
            m_BulletPrefab = GetSingleton<BulletAuthoringComponent>().Prefab;
            return;
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();

        var bulletVelocity = GetSingleton<GameSettingsComponent>().bulletVelocity;
        var bulletPrefab = m_BulletPrefab;
        var deltaTime = m_PredictionGroup.Time.DeltaTime;
        var currentTick = m_PredictionGroup.PredictingTick;

        var inputFromEntity = GetBufferFromEntity<PlayerCommand>(true);

        Entities
            .WithReadOnly(inputFromEntity)
            .WithAll<PlayerTag, PlayerCommand>()
            .ForEach((
                Entity entity,
                int nativeThreadIndex,
                ref PlayerStateAndOffsetComponent bulletOffset,
                in Rotation rotation,
                in Translation position,
                in VelocityComponent velocityComponent,
                in GhostOwnerComponent ghostOwner,
                in PredictedGhostComponent prediction
            ) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(currentTick, prediction))
                {
                    return;
                }

                var input = inputFromEntity[entity];

                PlayerCommand inputData;
                if (!input.GetDataAtTick(currentTick, out inputData)) inputData.shoot = 0;

                if (inputData.selfDestruct == 1)
                {
                    commandBuffer.AddComponent<DestroyTag>(nativeThreadIndex, entity);
                }

                var canShoot = bulletOffset.WeaponCooldown == 0 || SequenceHelpers.IsNewer(currentTick, bulletOffset.WeaponCooldown);
                if (inputData.shoot != 0 && canShoot)
                {
                    var bullet = commandBuffer.Instantiate(nativeThreadIndex, bulletPrefab);
                    commandBuffer.AddComponent(nativeThreadIndex, bullet, new PredictedGhostSpawnRequestComponent());

                    var newPosition = new Translation { Value = position.Value + math.mul(rotation.Value, bulletOffset.Value).xyz };
                    var vel = new PhysicsVelocity { Linear = bulletVelocity * math.mul(rotation.Value, new float3(0, 0, 1)).xyz + velocityComponent.Linear };

                    commandBuffer.SetComponent(nativeThreadIndex, bullet, newPosition);
                    commandBuffer.SetComponent(nativeThreadIndex, bullet, vel);
                    commandBuffer.SetComponent(nativeThreadIndex, bullet, new GhostOwnerComponent { NetworkId = ghostOwner.NetworkId });

                    bulletOffset.WeaponCooldown = currentTick + k_CoolDownTicksCount;
                }
            })
            .ScheduleParallel();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
