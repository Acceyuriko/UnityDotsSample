using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;

[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
public class ServerConnectionControl : SystemBase
{
    private ushort m_GamePort = 5001;

    private struct InitializeServerComponent : IComponentData { }

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<InitializeServerComponent>();
        EntityManager.CreateEntity(typeof(InitializeServerComponent));
    }

    protected override void OnUpdate()
    {
        EntityManager.DestroyEntity(GetSingletonEntity<InitializeServerComponent>());

        var grid = EntityManager.CreateEntity();
        EntityManager.AddComponentData(grid, new GhostDistanceImportance
        {
            ScaleImportanceByDistance = GhostDistanceImportance.DefaultScaleFunctionPointer,
            TileSize = new int3(80, 80, 80),
            TileCenter = new int3(0, 0, 0),
            TileBorderWidth = new float3(1f, 1f, 1f)
        });

        NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
        ep.Port = m_GamePort;
        World.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
        Debug.Log("Server is Listening on port " + m_GamePort.ToString());
    }
}

[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
public class ClientConnectionControl : SystemBase
{
    public string m_ConnectToServerIp = "127.0.0.1";
    public ushort m_GamePort = 5001;

    private struct InitializeClientComponent : IComponentData { }

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<InitializeClientComponent>();

        EntityManager.CreateEntity(typeof(InitializeClientComponent));
    }

    protected override void OnUpdate()
    {
        EntityManager.DestroyEntity(GetSingletonEntity<InitializeClientComponent>());

        NetworkEndPoint ep = NetworkEndPoint.Parse(m_ConnectToServerIp, m_GamePort);
        World.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
        Debug.Log("Client connecting to ip: " + m_ConnectToServerIp + " and port: " + m_GamePort.ToString());
    }
}