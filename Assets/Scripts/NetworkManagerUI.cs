using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton; 
    [SerializeField] private Button startServerButton;

    // Update is called once per frame
    private void Awake()
    {
        startHostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
   
        });

        startClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
       
        });

        startServerButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
          
        });
    }
}
