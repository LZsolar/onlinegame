using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.Assertions.Must;
using System.Security.Cryptography;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Unity.Services.Authentication;
using System.Linq;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine.Rendering;
public class LobbyScript : Singleton<LobbyScript>
{
    public Lobby hostLobby;
    public Lobby joinLobby;
    public string playerName;
    public string playerId;
    public float heartbeatTimer;
    public float lobbyUpdateTimer;
    public float lobbyPollTimer;
    public List<UILobby> listLobbyPanal;
    public Transform lobbyPanal;
    public GameObject prefabLobbyPanal;
    public TMP_InputField textNameLobby;
    public TMP_InputField textNamePlayer;
    public TMP_InputField textlobbyCode;
    public UIRoomLobby uIRoomLobby;
    public LoginManager loginManager;
    private void Start()
    {
        playerName = textNamePlayer.text;
    }
    private void Update()
    {
        if (!loginManager.isStart.Value)
        {
            HandleLobbyHeartbeat();
            HandleLobbyPollForUpdate();

        }
        HandleLobbyPolling();

    }

    private async void HandleLobbyHeartbeat()
    {
        try
        {
            if (hostLobby != null)
            {
                heartbeatTimer -= Time.deltaTime;
                if (heartbeatTimer < 0f)
                {
                    float heartbeatTimerMax = 15;
                    heartbeatTimer = heartbeatTimerMax;

                    await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void HandleLobbyPollForUpdate()
    {
        try
        {
            if (joinLobby != null)
            {
                lobbyUpdateTimer -= Time.deltaTime;

                if (lobbyUpdateTimer < 0f)
                {
                    Debug.Log("HandleLobby");
                    float lobbyUpdateTimerMax = 1.1f;
                    lobbyUpdateTimer = lobbyUpdateTimerMax;
                    Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinLobby.Id);
                    joinLobby = lobby;


                    RefeshUIRoomLobby(joinLobby);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void StartGame()
    {
        try
        {
            foreach (Player player in joinLobby.Players)
            {
                if (player.Data["Ready"].Value == "Not Ready")
                {
                    return;
                }
            }
            string relayCodeGame = await RelayManagerScript.Instance.CreateRelay();

            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>{
                    { "JoinCodeRelay",new DataObject(DataObject.VisibilityOptions.Public,relayCodeGame)}
                }
            });


            joinLobby = lobby;

            loginManager.uILobby.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void HandleLobbyPolling()
    {
        try
        {
            if (joinLobby != null)
            {
                lobbyPollTimer -= Time.deltaTime;
                if (lobbyPollTimer < 0)
                {
                    float lobbyPolllTimerMax = 1.1f;
                    lobbyPollTimer = lobbyPolllTimerMax;

                    joinLobby = await Lobbies.Instance.GetLobbyAsync(joinLobby.Id);

                    if (joinLobby.Data["JoinCodeRelay"].Value != "0" && playerId != joinLobby.HostId)
                    {
                        Debug.Log(" Client ready for Relat Cdoe : " + joinLobby.Data["JoinCodeRelay"].Value);
                        await RelayManagerScript.Instance.JoinRelay(joinLobby.Data["JoinCodeRelay"].Value);
                        joinLobby = null;
                        loginManager.uILobby.SetActive(false);
                    }
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void PressReadyOrNot()
    {
        try
        {
            Player player = joinLobby.Players.FirstOrDefault(player => player.Id == AuthenticationService.Instance.PlayerId);
            string ready = player.Data["Ready"].Value == "Ready" ? "Not Ready" : "Ready";
            await LobbyService.Instance.UpdatePlayerAsync(joinLobby.Id,
            playerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>{
                {"Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,ready)}
                }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        // }

        RefeshUIRoomLobby(joinLobby);
    }
    public async void CreateLobby()
    {
        try
        {
            if (textNameLobby == null)
                return;

            string lobbyName = "Welcome to Lobby!";
            playerName = textNamePlayer.text;
            int maxPlayers = 2;
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = false,
                Player = new Player()
                {
                    Data = new Dictionary<string, PlayerDataObject>{
                        {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName)},
                        {"Ready",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,"Not Ready")}
                    }
                },
                Data = new Dictionary<string, DataObject>{
                    {"GameMode",new DataObject(DataObject.VisibilityOptions.Public,"Public")},
                    {"JoinCodeRelay",new DataObject(DataObject.VisibilityOptions.Public,"0")}
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            hostLobby = lobby;
            playerId = AuthenticationService.Instance.PlayerId;
            joinLobby = hostLobby;

            // PrintPlayers(hostLobby);
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

            Debug.Log("Create Lobby " + lobby.Name + "," + lobby.MaxPlayers + " , " + lobby.Id + " , " + lobby.LobbyCode);

            Debug.Log(hostLobby.Data["JoinCodeRelay"].Value);

            RefeshUIRoomLobby(hostLobby);
            uIRoomLobby.uIStart.SetActive(true);
            loginManager.uICreateLobby.SetActive(false);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void UpdateLobbyAsync()
    {
        try
        {
            Lobby lobby = (hostLobby != null) ? hostLobby : joinLobby;

            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>{
                {"GameMode", new DataObject(DataObject.VisibilityOptions.Public,"Public")}
            }
            });
            joinLobby = hostLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void ListLobby()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter>()
            {
            new QueryFilter(
                field: QueryFilter.FieldOptions.AvailableSlots,
                op: QueryFilter.OpOptions.GT,
                value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder>()
            {
            new QueryOrder(
                asc: false,
                field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Lobby Found : " + lobbies.Results.Count);
            RefeshUIListLobby(lobbies);


        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public void RefeshUIRoomLobby(Lobby lobby)
    {
        // UpdateLobbyAsync();
        uIRoomLobby.gameObject.SetActive(true);
        uIRoomLobby.SetUpUIRoomLobby(lobby.LobbyCode, lobby.Name);
        uIRoomLobby.PrintPlayers(lobby);
    }

    public void RefeshUIListLobby(QueryResponse lobbies)
    {
        ClearUILobby();
        foreach (Lobby lobby in lobbies.Results)
        {
            GameObject objLobby = Instantiate(prefabLobbyPanal, lobbyPanal, true);
            UILobby uiLobby = objLobby.GetComponent<UILobby>();

            // Update to use the new setup function that properly uses the network variable.
            uiLobby.SetupLobbyUI(
                lobby.Name,
                lobby.Players.Count,
                lobby.MaxPlayers,
                "public", // Assuming status is always public for demonstration
                lobby.Id);

            Debug.Log("Lobby Name :" + lobby.Name + " Lobby Code : " + lobby.LobbyCode + "Lobby ID : " + lobby.Id
            + "Lobby RelayCode : " + lobby.Data["JoinCodeRelay"].Value);

            listLobbyPanal.Add(uiLobby);
            objLobby.SetActive(true);
        }
    }


    public void ClearUILobby()
    {
        foreach (var uiLobby in listLobbyPanal)
        {
            Destroy(uiLobby.gameObject);
        }
        listLobbyPanal.Clear();
    }
    private static IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }
    public async void JoinLobbyByCode()
    {
        try
        {
            // await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            if (textlobbyCode == null || string.IsNullOrEmpty(textlobbyCode.text))
            {
                Debug.LogError("Invalid lobby or lobby code");
                return;
            }
            Debug.Log(" Joined by lobby code : " + textlobbyCode.text);
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>{
                        {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName)},
                        {"Ready",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,"Not Ready")},

                    }
                }
            };


            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(textlobbyCode.text, options);
            joinLobby = lobby;
            Debug.Log(joinLobby.Data["JoinCodeRelay"].Value);


            playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log("Join by lobby code : " + textlobbyCode.text);
            RefeshUIRoomLobby(joinLobby);
            loginManager.uICreateLobby.SetActive(false);
            // ListLobby();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void JoinLobbyByID(UILobby uILobby)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>{
                        {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName)},
                        {"Ready",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,"Not Ready")}
                    }
                }
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(uILobby.lobbyID, options);
            joinLobby = lobby;

            Debug.Log(joinLobby.Data["JoinCodeRelay"].Value);

            playerId = AuthenticationService.Instance.PlayerId;
            RefeshUIRoomLobby(joinLobby);
            loginManager.uICreateLobby.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void LeaveLobby()
    {
        try
        {
            Debug.Log("LeaveLobby");

            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(joinLobby.Id, playerId);
            loginManager.isStart.Value = false;
            uIRoomLobby.gameObject.SetActive(false);
            loginManager.uILobby.gameObject.SetActive(true);
            hostLobby = null;
            joinLobby = null;

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void MigrateLobbyHost()
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinLobby.Players[1].Id,
            });
            joinLobby = hostLobby;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinLobby.Id);
            loginManager.isStart.Value = false;
            uIRoomLobby.gameObject.SetActive(false);
            loginManager.uILobby.gameObject.SetActive(true);

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
            Lobby lobby = await Lobbies.Instance.QuickJoinLobbyAsync();
            Debug.Log(lobby.Name + " , " + lobby.AvailableSlots);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

}
