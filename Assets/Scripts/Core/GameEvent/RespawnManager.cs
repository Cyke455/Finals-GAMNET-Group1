using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RespawnManager : NetworkBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float respawnDelay = 2f;

    public void RespawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        StartCoroutine(RespawnAfterDelay(clientId));
    }

    private IEnumerator RespawnAfterDelay(ulong clientId)
    {
        yield return new WaitForSeconds(respawnDelay);

        GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        NetworkObject networkObject = newPlayer.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true); // true = destroy existing player object
    }
}