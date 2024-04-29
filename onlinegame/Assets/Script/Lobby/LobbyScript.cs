using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;
using Mono.CSharp.Linq;
using QFSW.QC;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;

public class LobbyScript : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;

    [Header("Create")]
    public TMP_Text gameMode;
    public TMP_Text map;
    public TMP_Text time;

    [Header("Join")]
    public TMP_Text roomCode;

    [Header("Menu")]
    public GameObject CreateMenuObj;
    public GameObject JoinMenuObj;
    public GameObject MainMenu;
    public TMP_Text username;

    [Header("WaitingRoom")]
    public GameObject waitingRoom;
    public TMP_Text code;
    public TMP_Text playerList;
    public TMP_Text roominfo;


    // Update is called once per frame
    [Command]
    private async void CreateLobby()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        try
        {
            string lobbyName = "new lobby", gm = gameMode.text, m = map.text, t = time.text, user = username.text;
            int maxPlayers = 4;
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,

                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user)}
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    {"Gamemode", new DataObject(DataObject.VisibilityOptions.Public, gm)},
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public, m)},
                    {"Time", new DataObject(DataObject.VisibilityOptions.Public, t)}
                }

            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            hostLobby = lobby;
            joinedLobby = hostLobby;
            StartCoroutine(HeartBeatLobbyCoroutine(hostLobby.Id, 15));
            Debug.Log("Successful Lobby " + user + "   " + lobby.LobbyCode);
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private static IEnumerator HeartBeatLobbyCoroutine(string lobbyID, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyID);
            yield return delay;
        }
    }

    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            string user = username.text;
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user)}
                    }
                }
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            hostLobby = lobby;
            joinedLobby = hostLobby;
            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void QuickJoinLobby()
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void PrintPlayers(Lobby lobby)
    {
        playerList.text = "";
        int i = 1;
        foreach (Player player in lobby.Players)
        {
            playerList.text += i + ": " + player.Data["PlayerName"].Value + "\n";
        }
        code.text = "Lobby Code : " + lobby.LobbyCode;
        roominfo.text = "Mode : " + lobby.Data["Gamemode"].Value + "\n" + "Map : " + lobby.Data["Map"].Value + "\n" + "Time : " + lobby.Data["Time"].Value + "\n";
    }

    public void Click_Create()
    {
        CreateLobby();
        JoinMenuObj.SetActive(false);
        CreateMenuObj.SetActive(false);
        MainMenu.SetActive(false);

        waitingRoom.SetActive(true);
    }
    public void Click_Join() {
        JoinLobbyByCode(roomCode.ToString());

        JoinMenuObj.SetActive(false);
        CreateMenuObj.SetActive(false);
        MainMenu.SetActive(false);

        waitingRoom.SetActive(true);
    }
    public void Click_changeToCreate()
    {
        JoinMenuObj.SetActive(false);
        CreateMenuObj.SetActive(true);
        MainMenu.SetActive(false);

        waitingRoom.SetActive(false);
    }
    public void Click_changeToJoin()
    {
        JoinMenuObj.SetActive(true);
        CreateMenuObj.SetActive(false);
        MainMenu.SetActive(false);

        waitingRoom.SetActive(false);
    }
    public void Click_changeBack()
    {
        JoinMenuObj.SetActive(false);
        CreateMenuObj.SetActive(false);
        MainMenu.SetActive(true);

        waitingRoom.SetActive(false);
    }

    public async void StartGame(Lobby joinedLobby)
    {
        try
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId && player.Id == joinedLobby.HostId)
                {
                    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
                    string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
                    Debug.Log(relayServerData);
                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    if (transport != null)
                    {
                        transport.SetRelayServerData(relayServerData);
                    }
                    else
                    {
                        Debug.LogError("Failed to get UnityTransport component from NetworkManager.Singleton");
                        // Handle the error gracefully (e.g., display a message to the user)
                    }

                    Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject>
                            {
                                { "JoinCodeKey", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                            }
                    });
                    hostLobby = lobby;
                    joinedLobby = hostLobby;
                }
                else
                {
                    JoinRelay();
                }
            }
        }
        catch (LobbyServiceException e)
        {

            Debug.Log(e);
        }
    }

    public async void JoinRelay()
    {
        Debug.Log(joinedLobby.Data["JoinCodeKey"].Value);
        Debug.Log("Joining Relay with " + joinedLobby.Data["JoinCodeKey"].Value);
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinedLobby.Data["JoinCodeKey"].Value);
        RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetRelayServerData(relayServerData);
        }
        else
        {
            Debug.LogError("Failed to get UnityTransport component from NetworkManager.Singleton");
            // Handle the error gracefully (e.g., display a message to the user)
        }
    }
}
