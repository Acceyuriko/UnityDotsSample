using UnityEngine;

public class GameServerBroadcasting : MonoBehaviour
{
    private UdpConnection connection;

    public float perSecond = .5f;
    public float nextTime = 0;

    public ClientServerInfo ClientServerInfo;

    void Start()
    {
        if (!ClientServerInfo.IsServer)
        {
            this.enabled = false;
        }

        connection = new UdpConnection();
        connection.StartConnection(ClientServerInfo.BroadcastIpAddress, ClientServerInfo.BroadcastPort, ClientServerInfo.ReceivePort, true);
        Debug.Log($"server broadcasting at {ClientServerInfo.BroadcastIpAddress}:{ClientServerInfo.ReceivePort}");
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {
            connection.Send(nextTime, ClientServerInfo.GameName);
            nextTime += (1 / perSecond);
        }
    }

    void OnDestroy()
    {
        connection.Stop();
    }
}
