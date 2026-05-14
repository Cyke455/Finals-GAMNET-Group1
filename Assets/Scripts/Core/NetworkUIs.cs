using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using TMPro;

public class NetworkUIs : MonoBehaviour
{
    public TMP_InputField joinCodeInput; 
    public TextMeshProUGUI joinCodeText; 
    public TextMeshProUGUI statusText; 
    public GameObject startGameButton; 
    public string gameplaySceneName = "Stage 1"; 

    async void Start()
    {
        if (startGameButton != null) startGameButton.SetActive(false);
        if (statusText != null) statusText.text = "Initializing Services...";
        InitializationOptions options = new InitializationOptions();
        options.SetProfile(System.Guid.NewGuid().ToString().Substring(0, 8));
        await UnityServices.InitializeAsync(options);
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        if (statusText != null) statusText.text = "Online";
    }

    public async void StartHost()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            if (joinCodeText != null) joinCodeText.text = "Code: " + joinCode;
            if (statusText != null) statusText.text = "Waiting for Player 2...";

            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(allocation.ToRelayServerData("dtls"));
            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e) { Debug.LogError(e); }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            if (statusText != null) statusText.text = "Player 2 Joined! Ready?";
            if (startGameButton != null) startGameButton.SetActive(true);
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            if (statusText != null) statusText.text = "Player 2 Left. Waiting...";
            if (startGameButton != null) startGameButton.SetActive(false);
        }
    }

    public async void StartClient()
    {
        try
        {
            if (statusText != null) statusText.text = "Joining...";
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput.text);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e) 
        { 
            Debug.LogError(e); 
            if (statusText != null) statusText.text = "Error: Invalid Code";
        }
    }

    public void StartGame()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }
}