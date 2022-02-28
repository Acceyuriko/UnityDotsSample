using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
