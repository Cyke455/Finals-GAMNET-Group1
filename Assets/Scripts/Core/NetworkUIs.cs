using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUIs : MonoBehaviour
{

    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject button1;
    [SerializeField] private GameObject button2;
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        titleScreen.SetActive(false);
        button1.SetActive(false);
        button2.SetActive(false);
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        titleScreen.SetActive(false);
        button1.SetActive(false);
        button2.SetActive(false);
    }
}