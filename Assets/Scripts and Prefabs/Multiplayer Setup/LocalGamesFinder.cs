using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LocalGamesFinder : MonoBehaviour
{

    public UIDocument m_TitleUIDocument;


    private VisualElement m_titleScreenManagerVE;

    private TitleScreenManager m_titleScreenManagerClass;

    private ListView m_ListView;

    private List<ServerInfoObject> discoveredServerInfoObjects = new List<ServerInfoObject>();

    public VisualTreeAsset m_localGameListItemAsset;

    public float perSecond = 1.0f;
    private float nextTime = 0;

    public string BroadcastIpAddress = "255.255.255.255";
    public ushort BroadcastPort = 8014;
    public ushort ReceivePort = 8015;

    private UdpConnection connection;

    void OnEnable()
    {
        m_titleScreenManagerVE = m_TitleUIDocument.rootVisualElement;
        m_titleScreenManagerClass = m_titleScreenManagerVE.Q<TitleScreenManager>("TitleScreenManager");
        m_ListView = m_titleScreenManagerVE.Q<ListView>("local-games-list");
    }

    void Start()
    {
        connection = new UdpConnection();
        connection.StartConnection(BroadcastIpAddress, BroadcastPort, ReceivePort, false);
        connection.StartReceiveThread();

        m_ListView.makeItem = MakeItem;
        m_ListView.bindItem = BindItem;
        m_ListView.itemsSource = discoveredServerInfoObjects;
    }

    private VisualElement MakeItem()
    {
        VisualElement listItem = m_localGameListItemAsset.CloneTree();
        return listItem;
    }

    private void BindItem(VisualElement e, int index)
    {
        e.Q<Label>("game-name").text = discoveredServerInfoObjects[index].gameName;
        e.Q<Button>("join-local-game").RegisterCallback<ClickEvent>(ev => ClickedJoinGame(discoveredServerInfoObjects[index]));
    }

    void ClickedJoinGame(ServerInfoObject localGame)
    {
        m_titleScreenManagerClass.Q<JoinGameScreen>("JoinGameScreen").LoadJoinScreenForSelectedServer(localGame);
        m_titleScreenManagerClass.EnableJoinScreen();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {
            foreach (ServerInfoObject serverInfo in connection.getMessages())
            {
                ReceivedServerInfo(serverInfo);
            }

            nextTime += 1f / perSecond;
        }
    }

    private void ReceivedServerInfo(ServerInfoObject serverInfo)
    {
        bool ipExists = false;
        foreach (ServerInfoObject discoveredInfo in discoveredServerInfoObjects)
        {
            if (serverInfo.ipAddress == discoveredInfo.ipAddress)
            {
                ipExists = true;
                float receivedTime = float.Parse(serverInfo.timestamp);
                float storedTime = float.Parse(discoveredInfo.timestamp);

                if (receivedTime > storedTime)
                {
                    discoveredInfo.gameName = serverInfo.gameName;
                    discoveredInfo.timestamp = serverInfo.timestamp;
                    m_ListView.Refresh();
                }
            }
        }
        if (!ipExists)
        {
            discoveredServerInfoObjects.Add(serverInfo);
            m_ListView.Refresh();
        }
    }

    void OnDestroy()
    {
        connection.Stop();
    }
}
