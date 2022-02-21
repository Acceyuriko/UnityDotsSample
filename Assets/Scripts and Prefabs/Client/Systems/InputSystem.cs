using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public class InputSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    protected override void OnCreate()
    {
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        RequireSingletonForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {
        byte shoot;
        shoot = 0;

        if (Input.GetKey("space"))
        {
            shoot = 1;
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithAll<NetworkIdComponent>()
            .WithNone<NetworkStreamDisconnected>()
            .ForEach((Entity entity, int nativeThreadIndex, in CommandTargetComponent commandTargetComponent) =>
            {
                if (commandTargetComponent.targetEntity == Entity.Null)
                {
                    if (shoot != 0)
                    {
                        var req = commandBuffer.CreateEntity(nativeThreadIndex);
                        commandBuffer.AddComponent<PlayerSpawnRequestRpc>(nativeThreadIndex, req);
                        commandBuffer.AddComponent(nativeThreadIndex, req, new SendRpcCommandRequestComponent { TargetConnection = entity });
                    }
                }
            })
            .Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}
