using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MainGameManagerScript : MonoBehaviour
{
    public void OnServerButtonClick()
    {
        // Starts the NetworkManager as just a server
        NetworkManager.Singleton.StartServer();
    }

    public void OnHostButtonClick()
    {
        // Starts the NetworkManager as both a server and a client
        NetworkManager.Singleton.StartHost();
    }

    public void OnClientButtonClick()
    {
        // Starts the NetworkManager as just a client.
        NetworkManager.Singleton.StartClient();
    }

}
