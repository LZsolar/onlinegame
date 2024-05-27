using Unity.Netcode;
using TMPro;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using Unity.VisualScripting;
public class UIRoomLobby : NetworkBehaviour
{
    public TextMeshProUGUI textLobbyCode;
    public TextMeshProUGUI textLobbyName;
    public Transform panalplayer;
    public GameObject prefabDataPlayer;
    public List<UIPlayerData> listUIPlayerData;
    public GameObject uIStart;

    public void SetUpUIRoomLobby(string lobbyCode, string lobbyName)
    {
        textLobbyCode.text = "LobbyCode : " + lobbyCode;
        textLobbyName.text = "LobbyName : " + lobbyName;
    }
    
    public void PrintPlayers(Lobby lobby)
    {
        // Debug.Log("Lobby : " + lobby.Name + " / " + "LobbyJoinCodeRelay : " + lobby.Data["JoinCodeKey"].Value);
        ClearListPlayer();
        foreach (Player player in lobby.Players)
        {
            GameObject playerdata = Instantiate(prefabDataPlayer,panalplayer,true);

            UIPlayerData uIPlayerData = playerdata.GetComponent<UIPlayerData>();

            uIPlayerData.textPlayerName.text = player.Data["PlayerName"].Value;
            uIPlayerData.textPlayerStatus.text = player.Data["Ready"].Value;
            playerdata.SetActive(true);
            listUIPlayerData.Add(uIPlayerData);
            // Debug.Log(player.Id + " : " + player.Data["PlayerName"].Value + " : " + player.Data["Ready"].Value);
        }
    }
    public void ClearListPlayer()
    {   
        foreach (var item in listUIPlayerData)
        {
            Destroy(item.gameObject);
        }
        listUIPlayerData.Clear();
    }
}
