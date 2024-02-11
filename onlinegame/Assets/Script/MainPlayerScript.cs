using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class MainPlayerScript : NetworkBehaviour
{
    public float speed = 5.0f;
    public float rotationSpeed = 10.0f;
    Rigidbody rb;
    public TMP_Text namePrefab;
    private TMP_Text nameLabel;

    public GameObject eyesObject;

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
        nameLabel = Instantiate(namePrefab, Vector3.zero, Quaternion.identity) as TMP_Text;
        nameLabel.transform.SetParent(canvas.transform);
        posX.OnValueChanged += (int previousValue, int newValue) =>
        {
            Debug.Log("Owner ID = " + OwnerClientId + " : Pos X = " + posX.Value);
        };
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
        nameLabel.text = gameObject.name;
        nameLabel.transform.position = nameLabelPos;
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

        UpdatePlayerInfo();
        updateEyeColor();
    }

    private void UpdatePlayerInfo()
    {
        if (IsOwnedByServer) { nameLabel.text = playerNameA.Value.ToString(); }
        else { nameLabel.text = playerNameB.Value.ToString(); }
    }

    private void updateEyeColor()
    {
        loginManagerScript = GameObject.FindObjectOfType<LoginManagerScript>();

        if (IsOwnedByServer)
        {
            if (eyeStatus.Value == 0)
            {
                // print("EYE WHITE!!");
                eyesObject.GetComponent<Renderer>().material = loginManagerScript.materialList[0];
            }
            else if (eyeStatus.Value == 1)
            {
                // print("EYE RED!!");
                eyesObject.GetComponent<Renderer>().material = loginManagerScript.materialList[1];
            }
        }
        else
        {
            if (eyeStatus.Value == 0)
            {
                // print("EYE WHITE!!");
                eyesObject.GetComponent<Renderer>().material = loginManagerScript.materialList[0];
            }
            else if (eyeStatus.Value == 1)
            {
                // print("EYE RED!!");
                eyesObject.GetComponent<Renderer>().material = loginManagerScript.materialList[1];
            }
        }
    }

    public override void OnDestroy()
    {
        if (nameLabel != null) Destroy(nameLabel.gameObject);
        base.OnDestroy();
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {     
        if (IsOwner)
        {
            float translation = Input.GetAxis("Vertical") * speed;
            translation *= Time.deltaTime;
            rb.MovePosition(rb.position + this.transform.forward * translation);

            float rotation = Input.GetAxis("Horizontal");
            if (rotation != 0)
            {
                rotation *= rotationSpeed;
                Quaternion turn = Quaternion.Euler(0f, rotation, 0f);
                rb.MoveRotation(rb.rotation * turn);
            }
            else
            {
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
