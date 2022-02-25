using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.SceneManagement;

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
                    if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
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
}
