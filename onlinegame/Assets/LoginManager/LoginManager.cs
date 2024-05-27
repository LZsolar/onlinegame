using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
public class LoginManager : NetworkBehaviour
{
    public GameObject uILogin;
    public GameObject uILobby;
    public GameObject uICreateLobby;
    public LobbyScript lobbyScript;
    public float durationStartGame;
    public float timer = 0;
    public NetworkVariable<bool> isStart = new NetworkVariable<bool>();
    public bool isSpawn;
    public void SetUILogin(bool open)
    {
        uILogin.SetActive(open);
    }
    
    public void PreesLogin()
    {

        lobbyScript.playerName = lobbyScript.textNamePlayer.text;
        if (lobbyScript.playerName != " ")
        {
            uILogin.SetActive(false);
            uILobby.SetActive(true);
        }
    }
    private void Update()
    {
   
    }

    [ClientRpc]
    public void StartGameClientRpc(string joinCode)
    {
        Debug.Log(" Test Sent JoinCode " + joinCode);

        OnClientButtionClick(joinCode);
    }
    public void OnServerButtionClick()
    {
        bool isCanstart = true;

        // foreach (var player in lobbyScript.hostLobby.Players)
        // {
        //     if (player.Data["Ready"].Value == "False")
        //     {
        //         isCanstart = false;
        //     }
        // }

        if (isCanstart)
        {
            NetworkManager.Singleton.StartServer();

            SetUILogin(false);
        }
    }
    public void UpdateJoinCodeRelayInPlayerData()
    {
        foreach (Player player in lobbyScript.hostLobby.Players)
        {
            player.Data["JoinCodeRelay"].Value = joinCodeRelay;

        }
    }
    public async void OnHostButtionClick()
    {
        if (RelayManagerScript.Instance.IsRelayEnabled)
        {
            Debug.Log("In If RelaManaget");
            await RelayManagerScript.Instance.CreateRelay();
        }
        NetworkManager.Singleton.StartHost();
        UpdateJoinCodeRelayInPlayerData();
        SetUILogin(false);
    }
    public TMP_InputField joinCodeInputField;
    public string joinCodeRelay;
    public async void OnClientButtionClick(string joinCode)
    {
        joinCode = joinCodeInputField.GetComponent<TMP_InputField>().text;
        if (RelayManagerScript.Instance.IsRelayEnabled && !string.IsNullOrEmpty(joinCode))
        {
            await RelayManagerScript.Instance.JoinRelay(joinCode);
        }
        NetworkManager.Singleton.StartClient();
        SetUILogin(false);
    }

}
