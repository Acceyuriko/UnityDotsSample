using UnityEngine;
using UnityEngine.UIElements;

public class LocalGamesFinder : MonoBehaviour
{

    public UIDocument m_TitleUIDocument;
    public string BroadcastIpAddress = "255.255.255.255";
    public ushort BroadcastPort = 8014;

    private VisualElement m_titleScreenManagerVE;

    private TitleScreenManager m_titleScreenManagerClass;

    private ListView m_ListView;

    private GameObject[] discoveredServerInfoObjects;

    public VisualTreeAsset m_localGameListItemAsset;

    public float perSecond = 1.0f;
    private float nextTime = 0;

    void OnEnable()
    {
        m_titleScreenManagerVE = m_TitleUIDocument.rootVisualElement;
        m_titleScreenManagerClass = m_titleScreenManagerVE.Q<TitleScreenManager>("TitleScreenManager");
        m_ListView = m_titleScreenManagerVE.Q<ListView>("local-games-list");
    }

    void Start()
    {
        discoveredServerInfoObjects = GameObject.FindGameObjectsWithTag("LocalGame");

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
        e.Q<Label>("game-name").text = discoveredServerInfoObjects[index].name;
        e.Q<Button>("join-local-game").RegisterCallback<ClickEvent>(ev => ClickedJoinGame(discoveredServerInfoObjects[index]));
    }

    void ClickedJoinGame(GameObject localGame)
    {
        m_titleScreenManagerClass.Q<JoinGameScreen>("JoinGameScreen").LoadJoinScreenForSelectedServer(localGame);
        m_titleScreenManagerClass.EnableJoinScreen();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {
            discoveredServerInfoObjects = GameObject.FindGameObjectsWithTag("LocalGame");
            m_ListView.itemsSource = discoveredServerInfoObjects;
            m_ListView.Refresh();

            nextTime += 1f / perSecond;
        }
    }
}
