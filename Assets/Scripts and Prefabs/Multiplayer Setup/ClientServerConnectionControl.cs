using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;

[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
public class ServerConnectionControl : SystemBase
{
    private ushort m_GamePort;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<InitializeServerComponent>();
    }

    protected override void OnUpdate()
    {
        var serverDataEntity = GetSingletonEntity<ServerDataComponent>();
        var serverData = EntityManager.GetComponentData<ServerDataComponent>(serverDataEntity);
        m_GamePort = serverData.GamePort;

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
    public string m_ConnectToServerIp;
    public ushort m_GamePort;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<InitializeClientComponent>();
    }

    protected override void OnUpdate()
    {
        var clientDataEntity = GetSingletonEntity<ClientDataComponent>();
        var clientData = EntityManager.GetComponentData<ClientDataComponent>(clientDataEntity);

        m_ConnectToServerIp = clientData.ConnectToServerIp.ToString();
        m_GamePort = clientData.GamePort;

        EntityManager.DestroyEntity(GetSingletonEntity<InitializeClientComponent>());

        NetworkEndPoint ep = NetworkEndPoint.Parse(m_ConnectToServerIp, m_GamePort);
        World.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
        Debug.Log("Client connecting to ip: " + m_ConnectToServerIp + " and port: " + m_GamePort.ToString());
    }
}