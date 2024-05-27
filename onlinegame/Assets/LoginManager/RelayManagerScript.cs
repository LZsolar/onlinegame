using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Threading.Tasks;
// using ParrelSync;
public class RelayManagerScript : Singleton<RelayManagerScript>
{   

    private async void Start()
    {   
        // InitializationOptions options = new InitializationOptions();
    // #if UNITY_EDITOR
    //     options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
    // #endif
    
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signd in " + AuthenticationService.Instance.PlayerId);
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

    }
    public UnityTransport transport => NetworkManager.Singleton.GetComponent<UnityTransport>();
    public bool IsRelayEnabled => transport != null && transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport;
    public LoginManager loginManager;
    public LobbyScript  lobbyScript;
    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Join Code = " + joinCode);
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            loginManager.joinCodeRelay = joinCode;
            NetworkManager.Singleton.StartHost();

            return joinCode;
            
        }
        catch (RelayServiceException e) { Debug.Log(e); return null;}
    }
    public async Task JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e) { Debug.Log(e); }
    }
    
}
