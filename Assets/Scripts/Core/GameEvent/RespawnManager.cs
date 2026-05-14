using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RespawnManager : NetworkBehaviour
{
    [SerializeField] public Transform[] activeSpawnPoints; 
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float respawnDelay = 2f;

    public void UpdateSpawnPoints(Transform[] newPoints)
    {
        activeSpawnPoints = newPoints;
    }

    public void RespawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        LavaMoveUp[] allLava = FindObjectsByType<LavaMoveUp>(FindObjectsSortMode.None);
        foreach (var lava in allLava) 
        {
            lava.ResetLava();
        }

        ComputerPoint[] allComputers = FindObjectsByType<ComputerPoint>(FindObjectsSortMode.None);
        foreach (var comp in allComputers) 
        {
            comp.ResetComputer();
        }

        StartCoroutine(RespawnAfterDelay(clientId));
    }

    private IEnumerator RespawnAfterDelay(ulong clientId)
    {
        yield return new WaitForSeconds(respawnDelay);

        int safeIndex = (int)(clientId % (ulong)activeSpawnPoints.Length);
        Transform spawnPoint = activeSpawnPoints[safeIndex];

        GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        NetworkObject networkObject = newPlayer.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
    }
}