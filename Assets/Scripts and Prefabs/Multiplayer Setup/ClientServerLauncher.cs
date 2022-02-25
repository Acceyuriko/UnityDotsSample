using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;

public class ClientServerLauncher : MonoBehaviour
{
    public UIDocument m_TitleUIDocument;

    private VisualElement m_titleScreenManagerVE;
    private HostGameScreen m_HostGameScreen;
    private JoinGameScreen m_JoinGameScreen;
    private ManualConnectScreen m_ManualConnectScreen;

    void OnEnable()
    {
        m_titleScreenManagerVE = m_TitleUIDocument.rootVisualElement;
        m_HostGameScreen = m_titleScreenManagerVE.Q<HostGameScreen>("HostGameScreen");
        m_JoinGameScreen = m_titleScreenManagerVE.Q<JoinGameScreen>("JoinGameScreen");
        m_ManualConnectScreen = m_titleScreenManagerVE.Q<ManualConnectScreen>("ManualConnectScreen");

        m_HostGameScreen.Q("launch-host-game")?.RegisterCallback<ClickEvent>(ev => ClickedHostGame());
        m_JoinGameScreen.Q("launch-join-game")?.RegisterCallback<ClickEvent>(ev => ClickedJoinGame());
        m_ManualConnectScreen.Q("launch-connect-game")?.RegisterCallback<ClickEvent>(ev => ClickedJoinGame());
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void ClickedHostGame()
    {
        ServerLauncher();
        ClientLauncher();
        StartGameScene();
    }

    void ClickedJoinGame()
    {
        ClientLauncher();
        StartGameScene();
    }

    public void ServerLauncher()
    {
        var world = World.DefaultGameObjectInjectionWorld;
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
        ClientServerBootstrap.CreateServerWorld(world, "ServerWorld");
#endif
    }

    public void ClientLauncher()
    {
        var world = World.DefaultGameObjectInjectionWorld;

        int numClientWorlds = 1;
        int totalNumClients = numClientWorlds;

#if UNITY_EDITOR
        int numThinClients = ClientServerBootstrap.RequestedNumThinClients;
        totalNumClients += numThinClients;
#endif
        for (int i = 0; i < numClientWorlds; ++i)
        {
            ClientServerBootstrap.CreateClientWorld(world, "ClientWorld" + i);
        }
#if UNITY_EDITOR
        for (int i = numClientWorlds; i < totalNumClients; ++i)
        {
            var ClientWorld = ClientServerBootstrap.CreateClientWorld(world, "ClientWorld" + i);
            ClientWorld.EntityManager.CreateEntity(typeof(ThinClientComponent));
        }
#endif
    }

    void StartGameScene()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
            SceneManager.LoadSceneAsync("MainScene");
#if UNITY_EDITOR
        else
            Debug.Log("Loading: " + "MainScene");
#endif
    }
}
