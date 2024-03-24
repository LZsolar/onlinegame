using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;
using TMPro;
using System;
using Unity.Mathematics;
using Unity.Netcode.Transports.UTP;


public class LoginManagerScript : MonoBehaviour
{
    public List<uint> AlternativePlayerPrefabs;
    public TMP_Dropdown dropdown_TMP;

    public TMP_InputField userNameInputField;
    private bool isApproveConnection = false;
    [Command("set-approve")]

    public GameObject loginPanel;
    public GameObject leaveButton;
    public string ipAddress = "127.0.0.1";
    public TMP_InputField ipInputField;
    UnityTransport transport;
    public TMP_InputField joincodeInputField;
    public string joincode;

    [Header("SpawnPos")]
    [SerializeField] Transform[] posList;
   
    [Header("Material")]
    [SerializeField] public List<Material> materialList;

    public int roomID;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        loginPanel.SetActive(true);
        leaveButton.SetActive(false);
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        Debug.Log("HandleClientDisconnect = " + clientId);
        if (NetworkManager.Singleton.IsHost) { }
        else if (NetworkManager.Singleton.IsClient) { Leave(); }
    }
    public void Leave()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;

        }

        else if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }

        loginPanel.SetActive(true);
        leaveButton.SetActive(false);

    }
    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log("HandleHandleClientConnected = " + clientId);
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            loginPanel.SetActive(false);
            leaveButton.SetActive(true);
        }
    }

    private void HandleServerStarted()
    {
        Debug.Log("HandleServerStarted");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) { return; }
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    public bool SetIsApproveConnection()
    {
        isApproveConnection = !isApproveConnection;
        return isApproveConnection;
    }

    private void setIpAddress()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        ipAddress = ipInputField.GetComponent<TMP_InputField>().text;
        transport.ConnectionData.Address = ipAddress;
    }
    public async void Host()
    {
        //setIpAddress();
        if (RelayManagerScript.Instance.IsRelayEnabled)
        {
            await RelayManagerScript.Instance.CreateRelay();
        }
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.StartHost();
       /* roomID = int.Parse(roomIdInputField.GetComponent<TMP_InputField>().text);
        Debug.Log("Room id is: " + roomID);*/
        Debug.Log("Start host");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        int byteLength = connectionData.Length;
        bool isApprove = false;
        int characterPrefabIndex = 0;

        bool nameCheck = false;
        bool roomIDCheck = true;
        if (byteLength > 0)
        {
            string combinedString = System.Text.Encoding.ASCII.GetString(connectionData, 0, byteLength);
            string[] extractedString = HelperScript.ExtractStrings(combinedString);

            string hostData = userNameInputField.GetComponent<TMP_InputField>().text;
            nameCheck = NameApproveConnection(extractedString[0], hostData);
           // roomIDCheck = RoomIDApproveConnection(extractedString[2]);

            for (int i = 0; i < extractedString.Length; i++)
            {
                if (i == 0)
                {
                    string clientData = extractedString[i];
                    isApprove = NameApproveConnection(clientData, hostData);
                    if ((nameCheck == true) && (roomIDCheck == true))
                    {
                        isApprove = true;
                    }
                    else if ((nameCheck == false) && (roomIDCheck == false))
                    {
                        isApprove = false;
                        Debug.Log("Name is use alr && Room id is not correct");
                    }
                    else if ((nameCheck == false) || (roomIDCheck == false))
                    {
                        if (nameCheck == false)
                        {
                            isApprove = false;
                            Debug.Log("Name is use alr");
                        }
                        else
                        {
                            isApprove = false;
                            Debug.Log("Room id is not correct");
                        }
                    }
                }
                else if (i == 1)
                {
                    characterPrefabIndex = int.Parse(extractedString[i]);
                }
            }
        }

        else
        {
            //server
            if (NetworkManager.Singleton.IsHost)
            {
                string characterId = setInputSkinData().ToString();
                characterPrefabIndex = int.Parse(characterId);
            }
        }
        // Your approval logic determines the following values
        response.Approved = isApprove;
        response.CreatePlayerObject = true;

        // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
        //response.PlayerPrefabHash = null;

        response.PlayerPrefabHash = AlternativePlayerPrefabs[characterPrefabIndex];

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)

        response.Rotation = Quaternion.identity;

        SetSpawnLocation(clientId, response);
        NetworkLog.LogInfoServer("SpanwnPos of " + clientId + " is " + response.Position.ToString());
        NetworkLog.LogInfoServer("SpanwnRot of " + clientId + " is " + response.Rotation.ToString());

        // If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
        // On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
        response.Reason = "Some reason for not approving the client";

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
    }

    private void SetSpawnLocation(ulong clientId, NetworkManager.ConnectionApprovalResponse response)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        //server
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            int countOfAllPos = posList.Length;
            int randomPos = UnityEngine.Random.Range(0, countOfAllPos);
            Debug.Log("Random pos is : " + randomPos);
            spawnPos = new Vector3(posList[randomPos].position.x, posList[randomPos].position.y, posList[randomPos].position.z);
            spawnRot = Quaternion.Euler(posList[randomPos].eulerAngles.x, posList[randomPos].eulerAngles.y, posList[randomPos].eulerAngles.z);
        }
        else
        {
            int countOfAllPos = posList.Length;
            int randomPos = UnityEngine.Random.Range(0, countOfAllPos);
            Debug.Log("Random pos is : " + randomPos);
            spawnPos = new Vector3(posList[randomPos].position.x, posList[randomPos].position.y, posList[randomPos].position.z);
            spawnRot = Quaternion.Euler(posList[randomPos].eulerAngles.x, posList[randomPos].eulerAngles.y, posList[randomPos].eulerAngles.z);
            /*switch(NetworkManager.Singleton.ConnectedClients.Count)
            {
                case 1:
                    spawnPos = new Vector3(0f, 0f, 0f);
                    spawnRot = Quaternion.Euler(0f,180f, 0f);
                    break;
                case 2:
                    spawnPos = new Vector3(2f, 0f, 0f);
                    spawnRot = Quaternion.Euler(0f,225f, 0f);
                    break;
            }*/
        }
        response.Position = spawnPos;
        response.Rotation = spawnRot;
    }

    public async void Client()
    {
        joincode = joincodeInputField.GetComponent<TMP_InputField>().text;
        if (RelayManagerScript.Instance.IsRelayEnabled && !string.IsNullOrEmpty(joincode))
        {
            await RelayManagerScript.Instance.JoinRelay(joincode);
        }
        string username = userNameInputField.GetComponent<TMP_InputField>().text;
        string characterId = setInputSkinData().ToString();
        string[] inputFields = { username, characterId };
        string clientData = HelperScript.CombineStrings(inputFields);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(clientData);
        NetworkManager.Singleton.StartClient();

        Debug.Log("Start client");
    }

    public bool NameApproveConnection(string clientData, string hostData)
    {
        bool isApprove = System.String.Equals(clientData.Trim(), hostData.Trim()) ? false : true;
        Debug.Log("NameIsApprove = " + isApprove);
        return isApprove;
    }

    public bool RoomIDApproveConnection(string clientData)
    {
        bool isApprove;
        if (int.Parse(clientData) == roomID)
        {
            isApprove = true;
        }
        else
        {
            isApprove = false;
        }
        Debug.Log("RoomIDIsApprove = " + isApprove);
        return isApprove;
    }

    public int setInputSkinData()
    {

        if (dropdown_TMP.GetComponent<TMP_Dropdown>().value == 0)
        {
            return 0;
        }
        if (dropdown_TMP.GetComponent<TMP_Dropdown>().value == 1)
        {
            return 1;
        }
        if (dropdown_TMP.GetComponent<TMP_Dropdown>().value == 2)
        {
            return 2;
        }
        return 0;
    }
}