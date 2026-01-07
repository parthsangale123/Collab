using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string menuSceneName = "Menu"; 
    public string gameSceneName = "Game";

    [Header("UI Panels")]
    public GameObject mainPanel;      
    public GameObject hostPanel;      
    public GameObject clientPanel;    

    [Header("Inputs")]
    public TMP_Text codeText;         
    public TMP_InputField joinInput;  

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateGame()
    {
        try
        {
            // CHANGED: 1 means (1 Host + 1 Guest) = 2 Players Max
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            
            mainPanel.SetActive(false);
            hostPanel.SetActive(true);
            codeText.text = "Code: " + joinCode;
        }
        catch (System.Exception e) { Debug.LogError(e); }
    }

    public async void JoinGame()
    {
        string code = joinInput.text;
        if (string.IsNullOrEmpty(code)) return;

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

            mainPanel.SetActive(false);
            clientPanel.SetActive(true);
        }
        catch (System.Exception e) { Debug.LogError(e); }
    }

    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}