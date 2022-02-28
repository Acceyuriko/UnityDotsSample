using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UIElements;

public class HostGameScreen : VisualElement
{
    TextField m_GameName;
    Label m_GameIp;
    TextField m_PlayerName;
    String m_HostName = "";
    IPAddress m_MyIp;

    public new class UxmlFactory : UxmlFactory<HostGameScreen, UxmlTraits> { }

    public HostGameScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        // 
        // PROVIDE ACCESS TO THE FORM ELEMENTS THROUGH VARIABLES
        // 
        m_GameName = this.Q<TextField>("game-name");
        m_GameIp = this.Q<Label>("game-ip");
        m_PlayerName = this.Q<TextField>("player-name");

        m_HostName = Dns.GetHostName();

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
                        isInSubNet(addrInfo.Address, IPAddress.Parse("10.242.0.1"), IPAddress.Parse("255.255.0.0"))
                    )
                    {
                        m_MyIp = addrInfo.Address;
                    }
                }
            }
        }

        m_GameName.value = m_HostName;
        m_GameIp.text = m_MyIp.ToString();
        m_PlayerName.value = m_HostName;

        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    private bool isInSubNet(IPAddress ip, IPAddress subnet, IPAddress mask)
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
