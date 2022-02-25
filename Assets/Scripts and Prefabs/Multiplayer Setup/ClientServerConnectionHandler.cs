using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.NetCode;
using Unity.Entities;

public class ClientServerConnectionHandler : MonoBehaviour
{
    public ClientServerInfo ClientServerInfo;

    private GameObject[] launchObjects;

    public UIDocument m_GameUIDocument;
    private VisualElement m_GameManagerUIVE;

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

    }

    void ClickedQuitGame()
    {
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
        World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(World.DefaultGameObjectInjectionWorld.EntityManager.UniversalQuery);
        World.DisposeAllWorlds();

        var bootstrap = new NetCodeBootstrap();
        bootstrap.Initialize("defaultWorld");
    }
}
