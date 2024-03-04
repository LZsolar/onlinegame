using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class BananaScript : NetworkBehaviour
{
    public BananaSpawnerScript bananaSpawner;
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;
        if (collision.gameObject.tag == "Player")
        {
            ulong networkObjId = GetComponent<NetworkObject>().NetworkObjectId;
            bananaSpawner.DestroyServerRpc(networkObjId);
        }
    }

}