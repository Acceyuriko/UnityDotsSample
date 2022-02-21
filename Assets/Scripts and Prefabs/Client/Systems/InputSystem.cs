using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public class InputSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    private ClientSimulationSystemGroup m_ClientSimulationSystemGroup;

    private float m_PerSecond = 10f;
    private float m_NextTime = 0;

    private int m_FrameCount;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_ClientSimulationSystemGroup = World.GetOrCreateSystem<ClientSimulationSystemGroup>();

        RequireSingletonForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {
        bool isThinClient = HasSingleton<ThinClientComponent>();
        if (HasSingleton<CommandTargetComponent>() && GetSingleton<CommandTargetComponent>().targetEntity == Entity.Null)
        {
            if (isThinClient)
            {
                var ent = EntityManager.CreateEntity();
                EntityManager.AddBuffer<PlayerCommand>(ent);
                SetSingleton(new CommandTargetComponent { targetEntity = ent });
            }
        }

        byte right, left, thrust, reverseThrust, selfDestruct, shoot;
        right = left = thrust = reverseThrust = selfDestruct = shoot = 0;

        float mouseX = 0;
        float mouseY = 0;

        if (!isThinClient)
        {
            if (Input.GetKey("d")) right = 1;
            if (Input.GetKey("a")) left = 1;
            if (Input.GetKey("w")) thrust = 1;
            if (Input.GetKey("s")) reverseThrust = 1;
            if (Input.GetKey("p")) selfDestruct = 1;
            if (Input.GetKey("space")) shoot = 1;
            if (Input.GetMouseButton(1))
            {
                mouseX = Input.GetAxis("Mouse X");
                mouseY = Input.GetAxis("Mouse Y");
            }
        }
        else
        {
            var state = (int)Time.ElapsedTime % 3;
            shoot = 1;
            if (state == 0) left = 1;
            else thrust = 1;

            ++m_FrameCount;
            if (m_FrameCount % 100 == 0)
            {
                shoot = 1;
                m_FrameCount = 0;
            }
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();
        var inputFromEntity = GetBufferFromEntity<PlayerCommand>();
        var inputTargetTick = m_ClientSimulationSystemGroup.ServerTick;

        var canShoot = false;
        if (UnityEngine.Time.time >= m_NextTime)
        {
            canShoot = true;
            m_NextTime += (1 / m_PerSecond);
        }

        Entities
            .WithAll<NetworkIdComponent>()
            .WithNone<NetworkStreamDisconnected>()
            .ForEach((Entity entity, int nativeThreadIndex, in CommandTargetComponent commandTargetComponent) =>
            {
                if (isThinClient && shoot != 0)
                {
                    var req = commandBuffer.CreateEntity(nativeThreadIndex);
                    commandBuffer.AddComponent(nativeThreadIndex, req, new PlayerSpawnRequestRpc { });
                    commandBuffer.AddComponent(nativeThreadIndex, req, new SendRpcCommandRequestComponent { TargetConnection = entity });
                }

                if (commandTargetComponent.targetEntity == Entity.Null)
                {
                    if (shoot != 0)
                    {
                        var req = commandBuffer.CreateEntity(nativeThreadIndex);
                        commandBuffer.AddComponent(nativeThreadIndex, req, new PlayerSpawnRequestRpc { });
                        commandBuffer.AddComponent(nativeThreadIndex, req, new SendRpcCommandRequestComponent { TargetConnection = entity });
                    }
                }
                else
                {
                    if (inputFromEntity.HasComponent(commandTargetComponent.targetEntity))
                    {
                        var willShoot = shoot;
                        if (!canShoot && willShoot == 1)
                        {
                            willShoot = 0;
                        }

                        var input = inputFromEntity[commandTargetComponent.targetEntity];

                        input.AddCommandData(new PlayerCommand
                        {
                            Tick = inputTargetTick,
                            left = left,
                            right = right,
                            thrust = thrust,
                            reverseThrust = reverseThrust,
                            selfDestruct = selfDestruct,
                            shoot = willShoot,
                            mouseX = mouseX,
                            mouseY = mouseY
                        });
                    }
                }
            })
            .Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
