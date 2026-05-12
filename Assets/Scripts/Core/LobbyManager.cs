using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Lobby creation")]
    [SerializeField] private GameObject lobbyCreationParent;
    [SerializeField] private TMP_InputField createLobbyNameField;
    [SerializeField] private TMP_InputField createLobbyMaxPlayersField;

    [Space(10)]
    [Header("Lobby List")]
    [SerializeField] private GameObject lobbyListParent;
    [SerializeField] private Transform lobbyItemPrefab;
    [SerializeField] private Transform lobbyContentParent;

    [Space(10)]
    [Header("Profile setup")]
    [SerializeField] private GameObject profileSetupParent;
    [SerializeField] private TMP_InputField profileNameField;

    [Space(10)]
    [Header("Joined lobby")]
    [SerializeField] private GameObject joinedLobbyParent;
    [SerializeField] private Transform playerItemPrefab;
    [SerializeField] private Transform playerListParent;
    [SerializeField] private TextMeshProUGUI joinedLobbyNameText;
    [SerializeField] private GameObject joinedLobbyStartButton;


    private string playerName;
    private Player playerData;
    public string joinedLobbyId;

    private async void Start()
    {
        Instance = this;

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        profileSetupParent.SetActive(true);
        lobbyListParent.SetActive(false);
        joinedLobbyParent.SetActive(false);
        lobbyCreationParent.SetActive(false);
    }

    public void CreateProfile()
    {
        playerName = profileNameField.text;
        profileSetupParent.SetActive(false);
        lobbyListParent.SetActive(true);
        ShowLobbies();

        PlayerDataObject playerDataObjectName = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName);
        PlayerDataObject playerDataObjectTeam = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "A");

        playerData = new Player(id: AuthenticationService.Instance.PlayerId, data:
        new Dictionary<string, PlayerDataObject> { { "Name", playerDataObjectName }, { "Team", playerDataObjectTeam } });
    }

    public async void CreateLobby()
    {
        if (!int.TryParse(createLobbyMaxPlayersField.text, out int maxPlayers))
        {
            Debug.LogWarning("Incorrect player count");
            return;
        }

        Lobby createdLobby = null;

        try
        {
            createdLobby = await LobbyService.Instance.CreateLobbyAsync(createLobbyNameField.text, maxPlayers);
            joinedLobbyId = createdLobby.Id;
            UpdateLobbyInfo();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        LobbyHeartbeat(createdLobby);
    }

    private async void LobbyHeartbeat(Lobby lobby)
    {
        while(true)
        {
            if (lobby == null)
            {
                return;
            }

            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);

            await Task.Delay(15 * 1000);
        }
    }

    private async void ShowLobbies()
    {

        while (Application.isPlaying && lobbyListParent.activeInHierarchy)
        {
            try
            {
                QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

                foreach (Transform child in lobbyContentParent)
                {

                    if (child == lobbyItemPrefab) continue;
                    Destroy(child.gameObject);
                }

                foreach (Lobby lobby in queryResponse.Results)
                {
                    Transform newLobbyItem = Instantiate(lobbyItemPrefab, lobbyContentParent);

                    newLobbyItem.gameObject.SetActive(true);

                    newLobbyItem.GetComponent<JoinLobbyButton>().lobbyId = lobby.Id;
                    string info = $"{lobby.Name}, {lobby.Players.Count}/{lobby.MaxPlayers}";
                    newLobbyItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = info;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }

            await Task.Delay(3000);
        }
    }

    public async void JoinLobby(string lobbyID)
    {
        
        try
        {
            await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID, new JoinLobbyByIdOptions { Player = playerData });
            lobbyListParent.SetActive(false);
            joinedLobbyParent.SetActive(true);

            joinedLobbyId = lobbyID;
            UpdateLobbyInfo();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
    }


    private bool isJoined = false;
    private async void UpdateLobbyInfo()
    {
        while (Application.isPlaying)
        {
            if (string.IsNullOrEmpty(joinedLobbyId))
            {
                return;
            }

            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobbyId);

            if (AuthenticationService.Instance.PlayerId == lobby.HostId)
            {
                joinedLobbyStartButton.SetActive(true);
            }
            else
            {
                joinedLobbyStartButton.SetActive(false);
            }

            joinedLobbyNameText.text = lobby.Name;

            foreach (Transform t in playerListParent)
            {
                Destroy(t.gameObject);
            }

            foreach (Player player in lobby.Players)
            {
                Transform newPlayerItem = Instantiate(playerItemPrefab, playerListParent);
                newPlayerItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = player.Data["Name"].Value;
                newPlayerItem.GetChild(1).GetComponent<TextMeshProUGUI>().text = (lobby.HostId == player.Id) ? "Owner" : "User";
            }

            await Task.Delay(1000);
        }
    }

    public void ExitLobbyCreationButton()
    {
        lobbyCreationParent.SetActive(false);
        lobbyListParent.SetActive(true);
        ShowLobbies();
    }
    public void CreateNewLobbyButton()
    {
        lobbyCreationParent.SetActive(true);
        lobbyListParent.SetActive(false);
    }
}
