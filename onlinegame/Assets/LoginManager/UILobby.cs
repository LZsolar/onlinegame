using Unity.Netcode;
using TMPro;

public class UILobby : NetworkBehaviour 
{
    public TextMeshProUGUI textNameLobby;
    public TextMeshProUGUI textnumPlayer;
    public TextMeshProUGUI textStatusLobby;

    public string lobbyID ;

    // This function could be used to update the UI directly
    public void SetupLobbyUI(string name, int currentPlayerCount, int maxPlayers, string status, string idLobby)
    {
        textNameLobby.text = name;
        textnumPlayer.text = currentPlayerCount + "/" + maxPlayers;
        textStatusLobby.text = "Status: " + status;
        lobbyID = idLobby; // Use the .Value to assign the network variable.
    }
    
}
