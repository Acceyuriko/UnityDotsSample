using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;

public class ClientServerLauncher : MonoBehaviour
{
    public LocalGamesFinder GameBroadcasting;
    private string m_BroadcastIpAddress;
    private ushort m_BroadcastPort;

    public UIDocument m_TitleUIDocument;

    private VisualElement m_titleScreenManagerVE;
    private HostGameScreen m_HostGameScreen;
    private JoinGameScreen m_JoinGameScreen;
    private ManualConnectScreen m_ManualConnectScreen;

    public GameObject ServerLauncherObject;
    public GameObject ClientLauncherObject;

    public TextField m_GameName;
    public TextField m_GameIp;
    public Label m_GameIpLabel;
    public TextField m_PlayerName;

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
        m_BroadcastIpAddress = GameBroadcasting.BroadcastIpAddress;
        m_BroadcastPort = GameBroadcasting.BroadcastPort;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ClickedHostGame()
    {
        m_GameName = m_HostGameScreen.Q<TextField>("game-name");
        m_GameIpLabel = m_HostGameScreen.Q<Label>("game-ip");
        m_PlayerName = m_HostGameScreen.Q<TextField>("player-name");

        var gameName = m_GameName.value;
        var gameIp = m_GameIpLabel.text;
        var playerName = m_PlayerName.value;

        ServerLauncher(gameName);
        ClientLauncher(playerName, gameIp);
        StartGameScene();
    }

    void ClickedJoinGame()
    {
        m_GameIpLabel = m_JoinGameScreen.Q<Label>("game-ip");
        m_PlayerName = m_JoinGameScreen.Q<TextField>("player-name");

        var gameIp = m_GameIpLabel.text;
        var playerName = m_PlayerName.value;

        ClientLauncher(playerName, gameIp);
        StartGameScene();
    }

    void ClickedConnectGame()
    {
        m_GameIp = m_ManualConnectScreen.Q<TextField>("game-ip");
        m_PlayerName = m_ManualConnectScreen.Q<TextField>("player-name");

        var gameIp = m_GameIp.value;
        var playerName = m_PlayerName.value;

        ClientLauncher(playerName, gameIp);
        StartGameScene();
    }

    public void ServerLauncher(string gameName)
    {
        GameObject serverObject = Instantiate(ServerLauncherObject);
        DontDestroyOnLoad(serverObject);

        serverObject.GetComponent<ServerLaunchObjectData>().GameName = gameName;
        serverObject.GetComponent<ServerLaunchObjectData>().BroadcastIpAddress = m_BroadcastIpAddress;
        serverObject.GetComponent<ServerLaunchObjectData>().BroadcastPort = m_BroadcastPort;

        var world = World.DefaultGameObjectInjectionWorld;
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
        ClientServerBootstrap.CreateServerWorld(world, "ServerWorld");
#endif
    }

    public void ClientLauncher(string playerName, string ipAddress)
    {
        GameObject clientObject = Instantiate(ClientLauncherObject);
        DontDestroyOnLoad(clientObject);

        clientObject.GetComponent<ClientLaunchObjectData>().PlayerName = playerName;
        clientObject.GetComponent<ClientLaunchObjectData>().IPAddress= ipAddress;

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
