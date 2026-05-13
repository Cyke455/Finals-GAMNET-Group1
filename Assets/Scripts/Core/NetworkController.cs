using UnityEngine;
using Unity.Netcode;

public class NetworkController : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkManager.Singleton.StartHost();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Transform selectedSpawn = (clientId == NetworkManager.ServerClientId)
            ? hostSpawnPoint
            : clientSpawnPoint;

        GameObject playerInstance = Instantiate(playerPrefab, selectedSpawn.position, selectedSpawn.rotation);

        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}