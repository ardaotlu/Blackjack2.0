using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class IconActivater : NetworkBehaviour
{
    GameManager gameManager;
    public int playerNo = -1;
    public int indexNo = -1;


    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    void Update()
    {
        if (IsServer)
        {

        }
    }
}
