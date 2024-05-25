using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class MainPlayerScript : NetworkBehaviour
{
    Rigidbody rb;


    private NetworkVariable<int> posX = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public struct NetworkString : INetworkSerializable
    {
        public FixedString32Bytes info;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref info);
        }
        public override string ToString()
        {
            return info.ToString();
        }
        public static implicit operator NetworkString(string v) =>
            new NetworkString() { info = new FixedString32Bytes(v) };
    }

    private NetworkVariable<NetworkString> playerNameA = new NetworkVariable<NetworkString>(
        new NetworkString { info = "Player" },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<NetworkString> playerNameB = new NetworkVariable<NetworkString>(
    new NetworkString { info = "Player" },
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> eyeStatus = new NetworkVariable<int>(
   0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private LoginManagerScript loginManagerScript;

    public override void OnNetworkSpawn()
    {
        GameObject canvas = GameObject.FindWithTag("MainCanvas");
       
        //if (IsServer)
        //{
        //    playerNameA.Value = new NetworkString() { info = new FixedString32Bytes("Player1") };
        //    playerNameB.Value = new NetworkString() { info = new FixedString32Bytes("Player2") };
        //}

        if (IsOwner)
        {
            loginManagerScript = GameObject.FindObjectOfType<LoginManagerScript>();
            if (loginManagerScript != null)
            {
                string name = loginManagerScript.userNameInputField.text;
                if (IsOwnedByServer) { playerNameA.Value = name; }
                else { playerNameB.Value = name; }
            }
        }
    }

    private void updateEyeStatus()
    {
        if (IsOwner)
        {
            if (eyeStatus.Value == 0) { eyeStatus.Value = 1; }
            else { eyeStatus.Value = 0; }
        }
    }

    private void Update()
    {
        Vector3 nameLabelPos = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 2.5f, 0));
     
        if (IsOwner)
        {
            posX.Value = (int)System.Math.Ceiling(transform.position.x);

            if (IsOwnedByServer)
            {
                if (Input.GetKeyDown(KeyCode.F)) { updateEyeStatus(); }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.F)) { updateEyeStatus(); }
            }
        }

    }

  
  
    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
    }
}
