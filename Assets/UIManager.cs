using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI[] playerNames;
    [SerializeField] TextMeshProUGUI[] playersTotalMoneys;
    [SerializeField] Text statusText;


    [ClientRpc]
    public void SetReadyStatusClientRpc(int totalPlayers, int readyPlayers)
    {
        statusText.text = "Ready players: "+readyPlayers.ToString()+"/"+totalPlayers.ToString();
    }

    [ClientRpc]
    public void SetTotalClientRpc(int playerID,int total)
    {
        playersTotalMoneys[playerID].text=total.ToString();
    }
    
    [ClientRpc]
    public void SetNameClientRpc(int playerID, FixedString64Bytes name)
    {
        playerNames[playerID].text = name.ToString();
    }

    public int GetPlayerTotalMoney(int playerID)
    {
        return int.Parse(playersTotalMoneys[playerID].text);
    }
}
