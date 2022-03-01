using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientServerInfo : MonoBehaviour
{
    public bool IsServer = false;
    public bool IsClient = false;
    public string ConnectToServerIp;
    public ushort GamePort = 5001;

    public string GameName;
    public string PlayerName;

    public string BroadcastIpAddress;
    public ushort BroadcastPort;
    public ushort ReceivePort;

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
