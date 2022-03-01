using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;

public class ClientServerConnectionHandler : MonoBehaviour
{
    public ClientServerInfo ClientServerInfo;

    private GameObject[] launchObjects;

    public UIDocument m_GameUIDocument;
    private VisualElement m_GameManagerUIVE;

    private ClientSimulationSystemGroup m_ClientSimulationSystemGroup;
    private World m_ClientWorld;
    private EntityQuery m_ClientNetworkIdComponentQuery;
    private EntityQuery m_ClientDisconnectedNCEQuery;

    private World m_ServerWorld;
    private EntityQuery m_ServerNetworkIdComponentQuery;

    void OnEnable()
    {
        m_GameManagerUIVE = m_GameUIDocument.rootVisualElement;
        m_GameManagerUIVE.Q("quit-game")?.RegisterCallback<ClickEvent>(ev => ClickedQuitGame());
    }

    void Awake()
    {
        launchObjects = GameObject.FindGameObjectsWithTag("LaunchObject");
        foreach (GameObject launchObject in launchObjects)
        {
            if (launchObject.GetComponent<ServerLaunchObjectData>() != null)
            {
                ClientServerInfo.IsServer = true;
                ClientServerInfo.GameName = launchObject.GetComponent<ServerLaunchObjectData>().GameName;
                ClientServerInfo.BroadcastIpAddress = launchObject.GetComponent<ServerLaunchObjectData>().BroadcastIpAddress;
                ClientServerInfo.BroadcastPort = launchObject.GetComponent<ServerLaunchObjectData>().BroadcastPort;
                ClientServerInfo.ReceivePort = launchObject.GetComponent<ServerLaunchObjectData>().ReceivePort;

                foreach (var world in World.All)
                {
                    if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                    {
                        var ServerDataEntity = world.EntityManager.CreateEntity();
                        world.EntityManager.AddComponentData(ServerDataEntity, new ServerDataComponent
                        {
                            GameName = ClientServerInfo.GameName,
                            GamePort = ClientServerInfo.GamePort
                        });
                        world.EntityManager.CreateEntity(typeof(InitializeServerComponent));

                        m_ServerWorld = world;
                        m_ServerNetworkIdComponentQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkIdComponent>());
                    }
                }
            }
            if (launchObject.GetComponent<ClientLaunchObjectData>() != null)
            {
                ClientServerInfo.IsClient = true;
                ClientServerInfo.ConnectToServerIp = launchObject.GetComponent<ClientLaunchObjectData>().IPAddress;
                ClientServerInfo.PlayerName = launchObject.GetComponent<ClientLaunchObjectData>().PlayerName;

                foreach (var world in World.All)
                {
                    if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
                    {
                        var ClientDataEntity = world.EntityManager.CreateEntity();
                        world.EntityManager.AddComponentData(ClientDataEntity, new ClientDataComponent
                        {
                            PlayerName = ClientServerInfo.PlayerName,
                            ConnectToServerIp = ClientServerInfo.ConnectToServerIp,
                            GamePort = ClientServerInfo.GamePort
                        });
                        world.EntityManager.CreateEntity(typeof(InitializeClientComponent));

                        m_ClientWorld = world;
                        m_ClientSimulationSystemGroup = world.GetExistingSystem<ClientSimulationSystemGroup>();
                        m_ClientNetworkIdComponentQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkIdComponent>());
                        m_ClientDisconnectedNCEQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDisconnected>());
                    }
                }
            }
        }
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (m_ClientDisconnectedNCEQuery.IsEmptyIgnoreFilter)
            return;
        else
            ClickedQuitGame();
    }

    void ClickedQuitGame()
    {
        if (!m_ClientNetworkIdComponentQuery.IsEmptyIgnoreFilter)
        {
            var clientNCE = m_ClientSimulationSystemGroup.GetSingletonEntity<NetworkIdComponent>();
            m_ClientWorld.EntityManager.AddComponentData(clientNCE, new NetworkStreamRequestDisconnect());
        }

        if (m_ServerWorld != null)
        {
            var nceArray = m_ServerNetworkIdComponentQuery.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < nceArray.Length; i++)
            {
                m_ServerWorld.EntityManager.AddComponentData(nceArray[i], new NetworkStreamRequestDisconnect());
            }
            nceArray.Dispose();
        }
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
            SceneManager.LoadSceneAsync("NavigationScene");
#if UNITY_EDITOR
        else
            Debug.Log("Loading: " + "NavigationScene");
#endif
    }

    void OnDestroy()
    {
        for (var i = 0; i < launchObjects.Length; i++)
        {
            Destroy(launchObjects[i]);
        }

        Debug.Log("MainScene: ClientServerConnectionHandler OnDestroy");
        World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(World.DefaultGameObjectInjectionWorld.EntityManager.UniversalQuery);
        World.DisposeAllWorlds();

        var bootstrap = new NetCodeBootstrap();
        bootstrap.Initialize("defaultWorld");
    }
}
