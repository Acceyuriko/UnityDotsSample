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

        string sendToIp = ClientServerInfo.BroadcastIpAddress;
        int sendToPort = ClientServerInfo.BroadcastPort;

        connection = new UdpConnection();
        connection.StartConnection(sendToIp, sendToPort);
        Debug.Log($"server broadcasting at {sendToIp}:{sendToPort}");
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
