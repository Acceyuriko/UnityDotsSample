using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class UdpConnection
{
    private UdpClient udpClient;

    private string sendToIp;
    private int sendOrReceivePort;

    private readonly Queue<string> incomingQueue = new Queue<string>();
    private Thread receiveThread;
    private bool threadRunning = false;
    private IPAddress m_MyIp;

    public void StartConnection(string sendToIp, int sendOrReceivePort)
    {
        try
        {
            udpClient = new UdpClient(sendOrReceivePort);
        }
        catch (Exception e)
        {
            Debug.Log("Failed to listen for UDP at port " + sendOrReceivePort + ": " + e.Message);
            return;
        }

        m_MyIp = getMyIp();
        udpClient.EnableBroadcast = true;
        this.sendToIp = sendToIp;
        this.sendOrReceivePort = sendOrReceivePort;
    }

    public void StartReceiveThread()
    {
        receiveThread = new Thread(() => ListenForMessages(udpClient));
        receiveThread.IsBackground = true;
        threadRunning = true;
        receiveThread.Start();
    }

    private void ListenForMessages(UdpClient client)
    {
        IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (threadRunning)
        {
            try
            {
                Debug.Log("starting receive on " + m_MyIp.ToString() + " and port " + sendOrReceivePort.ToString());
                Byte[] receiveBytes = client.Receive(ref remoteIpEndPoint);
                string returnData = Encoding.UTF8.GetString(receiveBytes);

                lock (incomingQueue)
                {
                    incomingQueue.Enqueue(returnData);
                }
            }
            catch (SocketException e)
            {
                // 10004 thrown when socket is closed
                if (e.ErrorCode != 10004)
                {
                    Debug.Log("Socket exception while receiving data from udp client: " + e.Message);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error receiving data from udp client: " + e.Message);
            }

            Thread.Sleep(1);
        }
    }

    public ServerInfoObject[] getMessages()
    {
        ServerInfoObject[] pendingServerInfos = new ServerInfoObject[0];
        lock (incomingQueue)
        {
            pendingServerInfos = new ServerInfoObject[incomingQueue.Count];

            int i = 0;
            while (incomingQueue.Count != 0)
            {
                pendingServerInfos[i] = JsonUtility.FromJson<ServerInfoObject>(incomingQueue.Dequeue());
                i++;
            }
        }
        return pendingServerInfos;
    }

    public void Send(float floatTime, string gameName)
    {
        string stringTime = floatTime.ToString();
        IPEndPoint sendToEndPoint = new IPEndPoint(IPAddress.Parse(sendToIp), sendOrReceivePort);

        ServerInfoObject thisServerInfoObject = new ServerInfoObject();
        thisServerInfoObject.gameName = gameName;
        thisServerInfoObject.ipAddress = m_MyIp.ToString();
        thisServerInfoObject.timestamp = stringTime;

        string json = JsonUtility.ToJson(thisServerInfoObject);
        Byte[] sendBytes = Encoding.UTF8.GetBytes(json);
        udpClient.Send(sendBytes, sendBytes.Length, sendToEndPoint);
    }

    public void Stop()
    {
        if (threadRunning == true)
        {
            receiveThread.Abort();
            threadRunning = false;
        }
        udpClient.Close();
        udpClient.Dispose();
    }

    public static IPAddress getMyIp()
    {
        foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (
                netInterface.OperationalStatus == OperationalStatus.Up &&
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet
            )
            {
                foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                {
                    if (
                        addrInfo.Address.AddressFamily == AddressFamily.InterNetwork &&
                        UdpConnection.isInSubNet(addrInfo.Address, IPAddress.Parse("10.242.0.1"), IPAddress.Parse("255.255.0.0"))
                    )
                    {
                        return addrInfo.Address;
                    }
                }
            }

        }
        return null;
    }

    public static bool isInSubNet(IPAddress ip, IPAddress subnet, IPAddress mask)
    {
        byte[] ipAddressBytes = ip.GetAddressBytes();
        byte[] subnetAddressBytes = subnet.GetAddressBytes();
        byte[] maskBytes = mask.GetAddressBytes();

        byte[] address = new byte[ipAddressBytes.Length];
        for (int i = 0; i < address.Length; i++)
        {
            address[i] = (byte)(ipAddressBytes[i] & maskBytes[i]);
        }

        byte[] subnetAddress = new byte[subnetAddressBytes.Length];
        for (int i = 0; i < subnetAddress.Length; i++)
        {
            subnetAddress[i] = (byte)(subnetAddressBytes[i] & maskBytes[i]);
        }

        return new IPAddress(subnetAddress).Equals(new IPAddress(address));
    }
}

[Serializable]
public class ServerInfoObject
{
    public string gameName = "";
    public string ipAddress = "";
    public string timestamp = "";
}