using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private InputField joinCodeInput;
    public async void Host()
    {
        
        if (RelayManager.Instance.IsRelayEnabled)
            await RelayManager.Instance.SetupRelay();
        

        NetworkManager.Singleton.StartHost();
    }
    public async void Client()
    {
        
        if (RelayManager.Instance.IsRelayEnabled && !string.IsNullOrEmpty(joinCodeInput.text))
            await RelayManager.Instance.JoinRelay(joinCodeInput.text);
        

        NetworkManager.Singleton.StartClient();
    }
}
