using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class BananaSpawnerScript : NetworkBehaviour
{
    public GameObject bananaPrefab;
    private List<GameObject> spawnedBanana = new List<GameObject>();

    private OwnerNetworkAnimationScript ownerNetworkAnimationScript;

    private void Start()
    {
        ownerNetworkAnimationScript = GetComponent<OwnerNetworkAnimationScript>();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.K))
        {
            ownerNetworkAnimationScript.SetTrigger("BananaSpawn");
            SpawnBananaServerRpc();
        }
        else
        {
            ownerNetworkAnimationScript.ResetTrigger("BananaSpawn");
        }
    }

    [ServerRpc]
    void SpawnBananaServerRpc()
    {
        Vector3 spawnPos = transform.position + (transform.forward * 1.5f) + (transform.up * 1.5f);
        Quaternion spawnRot = transform.rotation;
        GameObject banana = Instantiate(bananaPrefab, spawnPos, spawnRot);
        spawnedBanana.Add(banana);
        banana.GetComponent<BananaScript>().bananaSpawner = this;
        banana.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyServerRpc(ulong networkObjId)
    {
        GameObject obj = findSpawnerBomb(networkObjId);
        if (obj == null) return;
        obj.GetComponent<NetworkObject>().Despawn();
        spawnedBanana.Remove(obj);
        Destroy(obj);
    }

    private GameObject findSpawnerBomb(ulong netWorkObjId)
    {
        foreach (GameObject bomb in spawnedBanana)
        {
            ulong bombId = bomb.GetComponent<NetworkObject>().NetworkObjectId;
            if (bombId == netWorkObjId) { return bomb; }
        }
        return null;
    }
}