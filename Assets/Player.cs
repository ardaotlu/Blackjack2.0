using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.Collections;
using JetBrains.Annotations;

public class Player : NetworkBehaviour
{
    [SerializeField] List<List<int>> playerHands=new List<List<int>>();
    Button HitBut = null;
    Button StandBut = null;
    Button SplitBut = null;
    Button DoubleBut= null;
    Button ResetBut = null;
    Button ReadyBut = null;
    Button SurrenderBut = null;

    Button But200 = null;
    Button But500 = null;
    Button But1000 = null;
    Button But2000 = null;

    int playerBet = 0;
    bool isReady = false;


    NetworkVariable<int> playerInitialTotalMoney = new NetworkVariable<int>(0);
    Text playerTotalMoneyGetText = null;
    NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>("");
    Text playerNameGetText = null;

    Text playerBetText = null;

    public int PlayerInitialTotalMoney { get { return playerInitialTotalMoney.Value; } }
    public FixedString64Bytes PlayerName { get { return playerName.Value; } }

    float timeSinceLastCheck = Mathf.Infinity;
    float checkTime = 0.2f;

    GameManager gameManager;
    public List<List<int>> PlayerHands { get { return playerHands; } set { playerHands = value; } }


    private void Start()
    {
        HitBut = GameObject.FindWithTag("HIT").GetComponent<Button>();
        HitBut.onClick.AddListener(() =>
        {
            Hit();
        });

        StandBut = GameObject.FindWithTag("STAND").GetComponent<Button>();
        StandBut.onClick.AddListener(() =>
        {
            Stand();
        });

        SplitBut = GameObject.FindWithTag("SPLIT").GetComponent<Button>();
        SplitBut.onClick.AddListener(() =>
        {
            SplitRequest();
        });

        DoubleBut = GameObject.FindWithTag("DOUBLE").GetComponent<Button>();
        DoubleBut.onClick.AddListener(() =>
        {
            DoubleRequest();
        });

        SurrenderBut = GameObject.FindWithTag("SURRENDER").GetComponent<Button>();
        SurrenderBut.onClick.AddListener(() =>
        {
            SurrenderRequest();
        });

        But200 = GameObject.FindWithTag("200").GetComponent<Button>();
        But200.onClick.AddListener(() =>
        {
            Increase(200);
        });
        But500 = GameObject.FindWithTag("500").GetComponent<Button>();
        But500.onClick.AddListener(() =>
        {
            Increase(500);
        });
        But1000 = GameObject.FindWithTag("1000").GetComponent<Button>();
        But1000.onClick.AddListener(() =>
        {
            Increase(1000);
        });
        But2000 = GameObject.FindWithTag("2000").GetComponent<Button>();
        But2000.onClick.AddListener(() =>
        {
            Increase(2000);
        });

        ResetBut = GameObject.FindWithTag("DECREASE").GetComponent<Button>();
        ResetBut.onClick.AddListener(() =>
        {
            Reset();
        });

        ReadyBut = GameObject.FindWithTag("READY").GetComponent<Button>();
        ReadyBut.onClick.AddListener(() =>
        {
            ReadyRequest();
        });
        



        playerNameGetText = GameObject.FindWithTag("PLAYERNAME").GetComponent<Text>();
        playerTotalMoneyGetText= GameObject.FindWithTag("PLAYERMONEY").GetComponent<Text>();
        playerBetText = GameObject.FindWithTag("BET").GetComponent<Text>();

        CloseButton(HitBut);
        CloseButton(StandBut);
        CloseButton(SplitBut);
        CloseButton(DoubleBut);
        CloseButton(SurrenderBut);


        gameManager = FindObjectOfType<GameManager>();

        List<int> playerHand = new List<int>();
        playerHands.Add(playerHand);

        StartCoroutine(SendDataToServer());

    }

    private IEnumerator SendDataToServer()
    {
        yield return new WaitForSeconds(0.2f);
        if (IsOwner)
        {
            SetDataServerRpc((int)OwnerClientId, playerNameGetText.text, int.Parse(playerTotalMoneyGetText.text));
            foreach(Canvas x in GameObject.FindWithTag("ENTRY").GetComponentsInChildren<Canvas>())
            {
                x.enabled = false;
            }

        }
    }

    [ServerRpc(RequireOwnership =false)]
    private void SetDataServerRpc(int senderID, FixedString64Bytes name,int money)
    {
        playerName.Value = name;
        playerInitialTotalMoney.Value = money;
    }

    private void Update()
    {

        if (IsOwner && timeSinceLastCheck > checkTime)
        {
            if (FindObjectOfType<GameManager>().PlayerTurn == (int)OwnerClientId)
            {
                OpenButton(HitBut);
                OpenButton(StandBut);
                CanSplitRequest();
                CanDoubleRequest();
                CanSurrenderRequest();

                if (FindObjectOfType<GameManager>().CanSplitCurrent)
                    OpenButton(SplitBut);
                else
                    CloseButton(SplitBut);

                if (FindObjectOfType<GameManager>().CanDoubleCurrent)
                    OpenButton(DoubleBut);
                else
                    CloseButton(DoubleBut);

                if (FindObjectOfType<GameManager>().CanSurrenderCurrent)
                    OpenButton(SurrenderBut);
                else
                    CloseButton(SurrenderBut);
            }
            else
            {
                CloseButton(HitBut);
                CloseButton(StandBut);
                CloseButton(SplitBut);
                CloseButton(DoubleBut);
                CloseButton(SurrenderBut);
            }

            if (FindObjectOfType<GameManager>().BetTime)
            {
                ReadyBut.GetComponentInParent<Canvas>().enabled = true;
                if(isReady)
                {
                    CloseButton(ReadyBut);
                }
                else
                {
                    OpenButton(ReadyBut);
                }
            }
            else
            {
                ReadyBut.GetComponentInParent<Canvas>().enabled = false;
                isReady = false;
            }
            timeSinceLastCheck = 0f;
        }


        if (IsOwner)
            timeSinceLastCheck += Time.deltaTime;
    }



    public void AddCard(int handNo,int index)
    {
        playerHands[handNo].Add(index);
    }

    public void Split(int handNo)
    {
        List<int> playerHand = new List<int>();
        playerHands.Add(playerHand);

        int num = playerHands[handNo][1];
        playerHands[playerHands.Count-1].Add(num);
        playerHands[handNo].RemoveAt(1);

        foreach(int i in playerHands[handNo])
        {
            Debug.Log("Hand " + handNo + " has a " + i);
        }
        foreach (int i in playerHands[handNo+1])
        {
            Debug.Log("Hand " + handNo + " has a " + i);
        }
    }

    private void Hit()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().HitRequestServerRpc((int)OwnerClientId);
    }
    private void Stand()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().StandRequestServerRpc((int)OwnerClientId);
    }

    private void SurrenderRequest()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().SurrenderRequestServerRpc((int)OwnerClientId);
    }

    private void CanSurrenderRequest()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().CanSurrenderRequestServerRpc((int)OwnerClientId);
    }

    private void SplitRequest()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().SplitRequestServerRpc((int)OwnerClientId);
    }

    private void CanSplitRequest()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().CanSplitRequestServerRpc((int)OwnerClientId);
    }

    private void DoubleRequest()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().DoubleRequestServerRpc((int)OwnerClientId);
    }

    private void CanDoubleRequest()
    {
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().CanDoubleRequestServerRpc((int)OwnerClientId);
    }

    private void ReadyRequest()
    {
        if (!IsOwner) return;
        if (FindObjectOfType<UIManager>().GetPlayerTotalMoney((int)OwnerClientId) >= playerBet && playerBet!=0)
        {
            FindObjectOfType<GameManager>().ReadyRequestServerRpc((int)OwnerClientId, playerBet);
            isReady = true;
        }
    }

    private void Increase(int amount)
    {
        if (!IsOwner) return;
        playerBet += amount;
        playerBetText.text=playerBet.ToString();

    }
    private void Reset()
    {
        if (!IsOwner) return;
        playerBet = 0;
        playerBetText.text = playerBet.ToString();
    }

    private void CloseButton(Button but)
    {
        but.GetComponent<Button>().enabled = false;
        but.GetComponent<Image>().enabled = false;
        but.GetComponentInChildren<Text>().enabled = false;
    }

    private void OpenButton(Button but)
    {
        but.GetComponent<Button>().enabled = true;
        but.GetComponent<Image>().enabled = true;
        but.GetComponentInChildren<Text>().enabled = true;
    }
}
