using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using System.Threading.Tasks;
using MongoDB.Driver;

public class GameManager : NetworkBehaviour
{
    // parameters
    [SerializeField] int delayTime = 700;
    [SerializeField] int startWaitTime = 10;
    [SerializeField] int endWaitTime = 8;


    // dont reset every turn
    [SerializeField] List<Player> players = new List<Player>();


    // Trying
    bool[] playerInGame = new bool[6] { false, false, false, false, false, false };
    int[] playerBetsInitial=new int[6] {0,0,0,0,0,0};
    bool[] surrendered= new bool[6] { false, false, false, false, false, false };

    [SerializeField] GameObject cardPrefab;
    [SerializeField] GameObject panelPrefab;
    [SerializeField] GameObject[] playerPanels;
    [SerializeField] GameObject dealerPanel;
    [SerializeField] GameObject[] turnIcons;
    [SerializeField] Text HighName;
    [SerializeField] Text HighScore;


    [SerializeField] List<FixedString64Bytes> playersNames= new List<FixedString64Bytes>();
    [SerializeField] List<int> playersTotalMoneys = new List<int>();

    NetworkVariable<int> P0Total = new NetworkVariable<int>(0);
    NetworkVariable<int> P1Total = new NetworkVariable<int>(0);
    NetworkVariable<int> P2Total = new NetworkVariable<int>(0);
    NetworkVariable<int> P3Total = new NetworkVariable<int>(0);
    NetworkVariable<int> P4Total = new NetworkVariable<int>(0);
    NetworkVariable<int> P5Total = new NetworkVariable<int>(0);

    List<int> myRandom = new List<int>();
    Hashtable deste = new Hashtable();
    int indexAtDeck = 0;

    // reset every turn
    [SerializeField] List<int> dealerHand=new List<int>();
    List<List<int>> playerTotals = new List<List<int>>();
    List<List<int>> playerBets = new List<List<int>>();

    NetworkVariable<int> playerTurn = new NetworkVariable<int>(-1);
    NetworkVariable<bool> betTime = new NetworkVariable<bool>(true);
    NetworkVariable<bool> canSplit = new NetworkVariable<bool>(false);
    NetworkVariable<bool> canDouble = new NetworkVariable<bool>(false);
    NetworkVariable<bool> canSurrender = new NetworkVariable<bool>(false);


    List<int> howManyHandsPlayer = new List<int>();
    List<int> howManyStandsPlayer = new List<int>();

    public int PlayerTurn { get { return playerTurn.Value; } }
    public bool BetTime { get { return betTime.Value; } }
    public bool CanSplitCurrent { get { return canSplit.Value; } }
    public bool CanDoubleCurrent { get { return canDouble.Value; } }
    public bool CanSurrenderCurrent { get { return canSurrender.Value; } }

    private bool gameStarted = false;
    private Coroutine currentCounter = null;
    [SerializeField] Text counterText = null;
    [SerializeField] Slider counterSlider = null;
    [SerializeField] Button startButton= null;


    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        foreach(GameObject g in turnIcons)
        {
            g.GetComponent<Image>().enabled = false;
        }
        startButton = GameObject.FindWithTag("START").GetComponent<Button>();
        startButton.onClick.AddListener(() =>
        {
            ServerStart();
        });
    }


    private void ServerStart()
    {
        if (!IsServer) return;
        currentCounter = StartCoroutine(GameStarter());
        CloseStartButtonClientRpc();
    }

    private IEnumerator GameStarter()
    {
        OpenTimerClientRpc();
        for (int i= startWaitTime; i > 0; i--)
        {
            UpdateTimerClientRpc(i, startWaitTime);
            yield return new WaitForSeconds(1);
        }
        StartGame();
        CloseTimerClientRpc();
    }

    private IEnumerator GameEnder()
    {
        PlayerScoreUpdateDB();
        yield return new WaitForSeconds(1);
        UpdateTableClientRpc();
        OpenTimerClientRpc();
        for (int i = endWaitTime; i > 0; i--)
        {
            UpdateTimerClientRpc(i, endWaitTime);
            yield return new WaitForSeconds(1);
        }
        CloseTimerClientRpc();
        yield return new WaitForSeconds(0.1f);
        RestartGame();
    }


    // ADD TO PLAYERS LIST
    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        if (!IsServer) return;
        if (obj == 0)
        {
            myRandom = GetComponent<DeckGenerator>().GenerateRandom();
            deste = GetComponent<DeckGenerator>().GenerateDeck();
        }
        StartCoroutine(AddPlayer(obj));
    }
    IEnumerator AddPlayer(ulong obj)
    {
        yield return new WaitForSeconds(0.3f);
        //Debug.Log("Player connected");

        foreach (Player player in FindObjectsOfType<Player>())
        {
            //Debug.Log("foreach: " + player.name);
            if (player.OwnerClientId == obj)
            {
                players.Add(player);
                playersNames.Add(player.PlayerName);
                AddMoneyToPlayer((int)obj, player.PlayerInitialTotalMoney);

                for(int i = 0; i < playersNames.Count; i++)
                {
                    FindObjectOfType<UIManager>().SetNameClientRpc(i, playersNames[i]);
                    FindObjectOfType<UIManager>().SetTotalClientRpc(i, playersTotalMoneys[i]);
                }

                FindObjectOfType<UIManager>().SetReadyStatusClientRpc(players.Count, HowManyReady());

            }
        }
    }
    /// end


    // FEATURES
    private void AddCardToPlayer(int handNo,int playerID, int cardID)
    {
        players[playerID].AddCard(handNo, cardID);
        CardImagePlayerClientRpc(handNo,playerID, cardID);
        indexAtDeck++;
        if (OverCheck(handNo, playerID)&& howManyStandsPlayer[playerID] + 1 == howManyHandsPlayer[playerID])
        {
            TurnStandIconOffClientRpc(playerTurn.Value, howManyStandsPlayer[playerTurn.Value]);

            if(GetNextActivePlayer() < players.Count)
                TurnStandIconOnClientRpc(GetNextActivePlayer(), howManyStandsPlayer[GetNextActivePlayer()]);

            EndTurn(playerID);
        }
        else if(OverCheck(handNo, playerID))
        {
            TurnStandIconOffClientRpc(playerTurn.Value, howManyStandsPlayer[playerTurn.Value]);
            howManyStandsPlayer[playerID]++;
            TurnStandIconOnClientRpc(playerTurn.Value, howManyStandsPlayer[playerTurn.Value]);
        }
    }

    private void AddCardToDealer(int cardID)
    {
        dealerHand.Add(cardID);
        CardImageDealerClientRpc(cardID);
        indexAtDeck++;
    }

    private void EndTurn(int playerID)
    {
        if (!IsServer) return;
        TurnIconOffClientRpc(playerID);
        if (GetNextActivePlayer() < players.Count)
        {
            TurnIconOnClientRpc(GetNextActivePlayer());

            playerTurn.Value= GetNextActivePlayer();
        }
        else if (GetNextActivePlayer() == players.Count)
        {
            OpenDealerCards();
            playerTurn.Value = -1;
        }
    }

    [ClientRpc]
    private void CardImagePlayerClientRpc(int panelNo,int playerID, int cardID)
    {
        GetComponent<AudioSource>().Play();
        Debug.Log("CardImage from player "+playerID+" to panel "+panelNo+" with cardID "+cardID);

        int imageID;
        if (cardID % 52 == 0)
            imageID = 52;
        else if (cardID > 52)
            imageID = cardID % 52;
        else
            imageID = cardID;

        GameObject img = Instantiate(cardPrefab, playerPanels[playerID].transform.GetChild(panelNo).transform.GetChild(0).transform);
        //img.gameObject.transform.localPosition += new Vector3(cardNo * 111.2f, 0, 0);
        //myStacks[handNo].GetComponent<RectTransform>().sizeDelta = new Vector2(110.2f + cardNo * 111.2f, 222f);
        string x = imageID.ToString();
        img.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(x);
    }

    [ClientRpc]
    private void PlayerPanelAddClientRpc(int playerID)
    {
        Instantiate(panelPrefab, playerPanels[playerID].transform);
    }

    [ClientRpc]
    private void CardImageDealerClientRpc(int cardID)
    {
        GetComponent<AudioSource>().Play();

        int imageID;
        if (cardID % 52 == 0)
            imageID = 52;
        else if (cardID > 52)
            imageID = cardID % 52;
        else
            imageID = cardID;

        GameObject img = Instantiate(cardPrefab, dealerPanel.transform);
        //img.gameObject.transform.localPosition += new Vector3(cardNo * 111.2f, 0, 0);
        //myStacks[handNo].GetComponent<RectTransform>().sizeDelta = new Vector2(110.2f + cardNo * 111.2f, 222f);
        string x = imageID.ToString();
        img.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(x);
    }

    [ClientRpc]
    private void CardImageDeleteAllPlayerClientRpc(int playerID)
    {
        foreach(Transform g in playerPanels[playerID].transform)
        {
            Destroy(g.gameObject);
        }
    }

    [ClientRpc]
    private void CardImageDeletePlayerClientRpc(int playerID,int handNo, int index)
    {
        Destroy(playerPanels[playerID].transform.GetChild(handNo).transform.GetChild(0).GetChild(index).gameObject);
    }

    [ClientRpc]
    private void CardImageDeleteDealerClientRpc()
    {
        foreach (Transform g in dealerPanel.transform)
        {
            Destroy(g.gameObject);
        }
    }

    [ClientRpc]
    private void TurnIconOnClientRpc(int playerID)
    {
        turnIcons[playerID].GetComponent<Image>().enabled = true;
    }

    [ClientRpc]
    private void TurnIconOffClientRpc(int playerID)
    {
        turnIcons[playerID].GetComponent<Image>().enabled = false;
    }

    [ClientRpc]
    private void TurnStandIconOnClientRpc(int playerID,int handNo)
    {
        playerPanels[playerID].transform.GetChild(handNo).transform.GetChild(1).GetComponent<Image>().enabled = true;
    }

    [ClientRpc]
    private void TurnStandIconOffClientRpc(int playerID, int handNo)
    {
        playerPanels[playerID].transform.GetChild(handNo).transform.GetChild(1).GetComponent<Image>().enabled = false;
    }


    [ClientRpc]
    private void UpdatePlayerTextClientRpc(int playerID, int handNo, FixedString64Bytes textToWrite)
    {
        TextMeshProUGUI text = playerPanels[playerID].transform.GetChild(handNo).transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        text.text = textToWrite.ToString();
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(int time, int maxTime)
    {
        counterSlider.maxValue=maxTime;
        counterText.text=time.ToString();
        counterSlider.value = time;
    }
    [ClientRpc]
    private void CloseTimerClientRpc()
    {
        counterSlider.gameObject.transform.parent.GetComponent<Canvas>().enabled = false;
    }
    [ClientRpc]
    private void OpenTimerClientRpc()
    {
        counterSlider.gameObject.transform.parent.GetComponent<Canvas>().enabled = true;
    }

    [ClientRpc]
    private void CloseStartButtonClientRpc()
    {
        startButton.GetComponent<Button>().enabled = false;
        startButton.GetComponent<Image>().enabled = false;
        startButton.GetComponentInChildren<Text>().enabled = false;
    }

    [ClientRpc]
    private void UpdateTableClientRpc()
    {
        GetHighScores();
    }

    [ServerRpc(RequireOwnership = false)]
    public void HitRequestServerRpc(int senderID)
    {
        AddCardToPlayer(howManyStandsPlayer[senderID], senderID, myRandom[indexAtDeck]);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StandRequestServerRpc(int senderID)
    {
        TurnStandIconOffClientRpc(playerTurn.Value, howManyStandsPlayer[playerTurn.Value]);
        howManyStandsPlayer[senderID]++;
        if (howManyStandsPlayer[senderID] == howManyHandsPlayer[senderID])
        {
            if (GetNextActivePlayer() < players.Count)
            {
                TurnStandIconOnClientRpc(GetNextActivePlayer(), howManyStandsPlayer[GetNextActivePlayer()]);
            }
            EndTurn(senderID);
        }
        else
        {
            TurnStandIconOnClientRpc(playerTurn.Value, howManyStandsPlayer[playerTurn.Value]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SurrenderRequestServerRpc(int senderID)
    {
        surrendered[senderID] = true;
        StandRequestServerRpc(senderID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanSurrenderRequestServerRpc(int senderID)
    {
        canSurrender.Value = CanSurrender(senderID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoubleRequestServerRpc(int senderID)
    {
        if (playersTotalMoneys[senderID] >= playerBets[senderID][howManyStandsPlayer[senderID]])
        {
            RemoveMoneyToPlayer(senderID, playerBets[senderID][howManyStandsPlayer[senderID]]);
            int doubleHand = howManyStandsPlayer[senderID];
            playerBets[senderID][howManyStandsPlayer[senderID]] *= 2;
            UpdatePlayerTextClientRpc(senderID, howManyStandsPlayer[senderID], playerBets[senderID][howManyStandsPlayer[senderID]].ToString());

            AddCardToPlayer(howManyStandsPlayer[senderID], senderID, myRandom[indexAtDeck]);
            if (!OverCheck(doubleHand, senderID))
                StandRequestServerRpc(senderID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanDoubleRequestServerRpc(int senderID)
    {
        canDouble.Value = CanDouble(senderID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SplitRequestServerRpc(int senderID)
    {
        if (playersTotalMoneys[senderID] >= playerBets[senderID][howManyStandsPlayer[senderID]])
        {
            StartCoroutine(Splitter(senderID));
            RemoveMoneyToPlayer(senderID, playerBets[senderID][howManyStandsPlayer[senderID]]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanSplitRequestServerRpc(int senderID)
    {
        canSplit.Value = CanSplit(senderID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyRequestServerRpc(int senderID, int value)
    {
        if(playersTotalMoneys[senderID] >= value)
        {
            playerInGame[senderID] = true;
            playerBetsInitial[senderID] = value;
            FindObjectOfType<UIManager>().SetReadyStatusClientRpc(players.Count, HowManyReady());
            if(HowManyReady()==players.Count)
            {
                StopCoroutine(currentCounter);
                StartGame();
                CloseTimerClientRpc();
            }
        }
    }

    private IEnumerator Splitter(int senderID)
    {
        playerBets[senderID].Add(playerBets[senderID][howManyStandsPlayer[senderID]]);

        howManyHandsPlayer[senderID]++;
        PlayerPanelAddClientRpc(senderID);
        players[senderID].Split(howManyStandsPlayer[senderID]);
        CardImageDeletePlayerClientRpc(senderID, howManyStandsPlayer[senderID],1);
        yield return new WaitForSeconds(0.1f);
        UpdatePlayerTextClientRpc(senderID, howManyHandsPlayer[senderID] - 1, playerBets[senderID][howManyHandsPlayer[senderID] - 1].ToString());
        CardImagePlayerClientRpc(howManyHandsPlayer[senderID] - 1, senderID, players[senderID].PlayerHands[howManyHandsPlayer[senderID] - 1][0]);

        TurnStandIconOffClientRpc(senderID, howManyHandsPlayer[senderID] - 1);
        AddCardToPlayer(howManyStandsPlayer[senderID], senderID, myRandom[indexAtDeck]);
        AddCardToPlayer(howManyHandsPlayer[senderID] - 1, senderID, myRandom[indexAtDeck]);

    }


    /// end

    /// CALCULATIONS
    private int GetPlayerTotal(int handNo,int playerID)
    {
        int total = 0;
        foreach(int i in players[playerID].PlayerHands[handNo])
        {
            int value = ((Kart)deste[i]).Mag;

            if (value < 11)
                total += value;
            else if (value > 10 && value < 14)
                total += 10;
            else
            {
                total += 1;
            }
        }
        Debug.Log("Player " + playerID + " total: " + total);
        return total;
    }

    private int GetPlayerTotalEndGame(int handNo, int playerID)
    {
        int total = 0;
        foreach (int i in players[playerID].PlayerHands[handNo])
        {
            int value = ((Kart)deste[i]).Mag;

            if (value < 11)
                total += value;
            else if (value > 10 && value < 14)
                total += 10;
            else
            {
                total += 1;
            }
        }

        foreach (int i in players[playerID].PlayerHands[handNo])
        {
            if (((Kart)deste[i]).Mag == 14 && total + 10 < 22)
                total += 10;
        }

        return total;
    }

    private int GetDealerTotal()
    {
        int total = 0;
        foreach (int i in dealerHand)
        {
            int value = ((Kart)deste[i]).Mag;

            if (value < 11)
                total += value;
            else if (value > 10 && value < 14)
                total += 10;
            else
            {
                total += 1;
            }
        }
        foreach (int j in dealerHand)
        {
            if (((Kart)deste[j]).Mag == 14 && total + 10 > 16 && total + 10 < 22)
            {
                total += 10;
            }
        }
        return total;
    }
    private bool OverCheck(int handNo,int playerID)
    {
        if (GetPlayerTotal(handNo,playerID) > 21)
        {
            return true;
        }
        else return false;
    }

    private bool CanSplit(int playerID)
    {
        List<int> list = new List<int>();
        foreach(int i in players[playerID].PlayerHands[howManyStandsPlayer[playerID]])
        {
            int mag = 0;

            int value = ((Kart)deste[i]).Mag;

            if (value < 11)
                mag= value;
            else if (value > 10 && value < 14)
                mag= 10;
            else
            {
                mag= 1;
            }
            list.Add(mag);
        }

        if (list.Count==2&&list[0] == list[1])
        {
            return true;
        }
        else 
            return false;

    }
    private bool CanDouble(int playerID)
    {
        if (players[playerID].PlayerHands[howManyStandsPlayer[playerID]].Count == 2)
        {
            return true;
        }
        else
            return false;
    }

    private bool CanSurrender(int playerID)
    {
        if (players[playerID].PlayerHands.Count == 1 && players[playerID].PlayerHands[howManyStandsPlayer[playerID]].Count == 2 && ((Kart)deste[dealerHand[0]]).Mag!=14)
        {
            return true;
        }
        else
            return false;
    }

    private void AddMoneyToPlayer(int playerNo,int sum)
    {
        if (playerNo < playersTotalMoneys.Count)
        {
            playersTotalMoneys[playerNo] += sum;
        }
        else
            playersTotalMoneys.Add(sum);

        if (playerNo == 0)
        {
            P0Total.Value += sum;
        }
        else if (playerNo == 1)
        {
            P1Total.Value += sum;
        }
        else if (playerNo == 2)
        {
            P2Total.Value += sum;
        }
        else if (playerNo == 3)
        {
            P3Total.Value += sum;
        }
        else if (playerNo == 4)
        {
            P4Total.Value += sum;
        }
        else if (playerNo == 5)
        {
            P5Total.Value += sum;
        }

        FindObjectOfType<UIManager>().SetTotalClientRpc(playerNo, playersTotalMoneys[playerNo]);

    }

    private void RemoveMoneyToPlayer(int playerNo, int sum)
    {
        if (playerNo < playersTotalMoneys.Count)
        {
            playersTotalMoneys[playerNo] -= sum;
        }

        if (playerNo == 0)
        {
            P0Total.Value -= sum;
        }
        else if (playerNo == 1)
        {
            P1Total.Value -= sum;
        }
        else if (playerNo == 2)
        {
            P2Total.Value -= sum;
        }
        else if (playerNo == 3)
        {
            P3Total.Value -= sum;
        }
        else if (playerNo == 4)
        {
            P4Total.Value -= sum;
        }
        else if (playerNo == 5)
        {
            P5Total.Value -= sum;
        }

        FindObjectOfType<UIManager>().SetTotalClientRpc(playerNo, playersTotalMoneys[playerNo]);

    }

    private int GetNextActivePlayer()
    {
        int nextActivePlayer = playerTurn.Value + 1;
        while (nextActivePlayer < players.Count)
        {
            if (!playerInGame[nextActivePlayer])
            {
                nextActivePlayer++;
                continue;
            }
            else
            {
                break;
            }
        }
        return nextActivePlayer;
    }

    private int HowManyReady()
    {
        int ready = 0;
        foreach(bool b in playerInGame)
        {
            if (b) ready++;
        }
        return ready;
    }
    /// end


    // MANAGE GAME
    private async void StartGame()
    {
        betTime.Value = false;

        for (int i = 0; i < players.Count; i++)
        {
            howManyHandsPlayer.Add(1);
            howManyStandsPlayer.Add(0);
            PlayerPanelAddClientRpc(i);
            TurnStandIconOffClientRpc(i, 0);
            List<int> subBets = new List<int>();
            playerBets.Add(subBets);

            if (!playerInGame[i])
                continue;

            await Task.Delay(delayTime);
            playerBets[i].Add(playerBetsInitial[i]);
            UpdatePlayerTextClientRpc(i, 0, playerBets[i][0].ToString());
            RemoveMoneyToPlayer(i, playerBets[i][0]);

            AddCardToPlayer(0,i, myRandom[indexAtDeck]);
        }
        await Task.Delay(delayTime);
        AddCardToDealer(myRandom[indexAtDeck]);
        for (int i = 0; i < players.Count; i++)
        {
            if (!playerInGame[i])
                continue;
            await Task.Delay(delayTime);
            AddCardToPlayer(0,i, myRandom[indexAtDeck]);
        }
        StartCoroutine(UpdateAndShowDB());
        playerTurn.Value= GetNextActivePlayer();
        Debug.Log(playerTurn.Value);
        TurnIconOnClientRpc(playerTurn.Value);
        TurnStandIconOnClientRpc(playerTurn.Value, howManyStandsPlayer[playerTurn.Value]);
    }

    private async void OpenDealerCards()
    {
        int dealerTotal = GetDealerTotal();
        while (dealerTotal < 17)
        {
            await Task.Delay(delayTime);
            AddCardToDealer(myRandom[indexAtDeck]);
            dealerTotal = GetDealerTotal();
        }
        EndGame();
    }

    private void EndGame()
    {
        for(int i=0; i<players.Count; i++)
        {
            List<int> subTotals = new List<int>();
            playerTotals.Add(subTotals);

            if (playerInGame[i]) 
            {
                for (int j = 0; j < howManyHandsPlayer[i]; j++)
                {
                    playerTotals[i].Add(GetPlayerTotalEndGame(j, i));
                }
            }
        }
        int dealerTotal = GetDealerTotal();

        for (int i = 0; i < players.Count; i++)
        {
            if (!playerInGame[i]) continue;

            if (surrendered[i])
            {
                // surrendered
                AddMoneyToPlayer(i, playerBets[i][0]/2);
                UpdatePlayerTextClientRpc(i, 0, (playerBets[i][0]/2).ToString()+"\nSURRENDER");
                continue;
            }

            for (int j=0; j < howManyHandsPlayer[i]; j++)
            {
                if (playerTotals[i][j] > 21)
                {
                    // lost
                    UpdatePlayerTextClientRpc(i, j,"0\nLOST");
                }
                else if (playerTotals[i][j] == 21 && players[i].PlayerHands[j].Count == 2)
                {
                    if(dealerTotal==21&&dealerHand.Count==2)
                    {
                        // tie
                        AddMoneyToPlayer(i, playerBets[i][j]);
                        UpdatePlayerTextClientRpc(i, j, playerBets[i][j].ToString() + "\nTIE");
                    }
                    else
                    {
                        // blackjack
                        AddMoneyToPlayer(i, (playerBets[i][j] * 5) / 2);
                        UpdatePlayerTextClientRpc(i, j, ((playerBets[i][j] * 5) / 2).ToString()+"\nBLACKJACK");
                    }
                }
                else if (dealerTotal == 21 && dealerHand.Count == 2)
                {
                    // lost
                    UpdatePlayerTextClientRpc(i, j, "0\nLOST");
                }
                else if (dealerTotal > 21)
                {
                    // win
                    AddMoneyToPlayer(i, playerBets[i][j] * 2);
                    UpdatePlayerTextClientRpc(i, j, (playerBets[i][j] * 2).ToString()+"\nWIN");
                }
                else if (dealerTotal == playerTotals[i][j])
                {
                    // tie
                    AddMoneyToPlayer(i, playerBets[i][j]);
                    UpdatePlayerTextClientRpc(i, j, playerBets[i][j].ToString() + "\nTIE");
                }
                else if (dealerTotal > playerTotals[i][j])
                {
                    // lose
                    UpdatePlayerTextClientRpc(i, j, "0\nLOST");
                }
                else if (dealerTotal < playerTotals[i][j])
                {
                    // win
                    AddMoneyToPlayer(i, playerBets[i][j] * 2);
                    UpdatePlayerTextClientRpc(i, j, (playerBets[i][j] * 2).ToString() + "\nWIN");
                }
            }

        }

        currentCounter = StartCoroutine(GameEnder());
    }

    private void RestartGame()
    {
        Clearer();
    }

    private void Clearer()
    {
        playerTurn.Value = -1;
        for (int i=0;i<players.Count;i++)
        {
            for (int j = 0; j < howManyHandsPlayer[i]; j++)
            {
                UpdatePlayerTextClientRpc(i,j, "");
            }
            TurnIconOffClientRpc(i);
            CardImageDeleteAllPlayerClientRpc(i);
            players[i].PlayerHands= new List<List<int>>();
            List<int> playerHand = new List<int>();
            players[i].PlayerHands.Add(playerHand);
        }
        CardImageDeleteDealerClientRpc();
        playerTotals.Clear();
        dealerHand.Clear();
        howManyHandsPlayer.Clear();
        howManyStandsPlayer.Clear();
        playerInGame = new bool[6] { false, false, false, false, false, false };
        surrendered = new bool[6] { false, false, false, false, false, false };
        betTime.Value = true;
        playerBetsInitial = new int[6] { 0, 0, 0, 0, 0, 0 };
        playerBets.Clear();
        FindObjectOfType<UIManager>().SetReadyStatusClientRpc(players.Count, HowManyReady());
        if (indexAtDeck >= 230)
        {
            myRandom = GetComponent<DeckGenerator>().GenerateRandom();
            deste = GetComponent<DeckGenerator>().GenerateDeck();
        }
        currentCounter = StartCoroutine(GameStarter());
    }

    private IEnumerator UpdateAndShowDB()
    {
        PlayerScoreUpdateDB();
        yield return new WaitForSeconds(1);
        UpdateTableClientRpc();
    }

    // database features
    async void GetHighScores()
    {
        Dictionary<string, int> playerScoreDb = new Dictionary<string, int>();
        const string connectionUri = "mongodb+srv://arda:admin@cluster0.qcgp3dk.mongodb.net/?retryWrites=true&w=majority";
        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        // Create a new client and connect to the server
        var client = new MongoClient(settings);
        string databaseName = "gameDB";
        string collectionName = "Player";
        var db = client.GetDatabase(databaseName);
        var collection = db.GetCollection<PlayerID>(collectionName);

        var results = await collection.FindAsync(_ => true);

        foreach (var result in results.ToList())
        {
            playerScoreDb.Add(result.Name, int.Parse(result.Score));
        }

        var orderedDict = from entry in playerScoreDb orderby entry.Value descending select entry;

        Dictionary<string, int> orderedPlayerScoreDb = orderedDict.ToDictionary<KeyValuePair<string, int>, string, int>(pair => pair.Key, pair => pair.Value);
        string Names = "";
        string Scores = "";
        int l = 1;
        foreach (string result in orderedPlayerScoreDb.Keys)
        {
            Names += l.ToString() + ". " + result + "\n";
            Scores += orderedPlayerScoreDb[result] + "\n";
            l++;
        }
        HighName.text = Names;
        HighScore.text = Scores;
    }

    private async void PlayerScoreUpdateDB()
    {
        Dictionary<string, int> playerScoreDb = new Dictionary<string, int>();

        const string connectionUri = "mongodb+srv://arda:admin@cluster0.qcgp3dk.mongodb.net/?retryWrites=true&w=majority";
        var settings = MongoClientSettings.FromConnectionString(connectionUri);

        // Create a new client and connect to the server
        var client = new MongoClient(settings);
        string databaseName = "gameDB";
        string collectionName = "Player";
        var db = client.GetDatabase(databaseName);
        var collection = db.GetCollection<PlayerID>(collectionName);

        for (int i = 0; i < players.Count; i++)
        {
            var filter = Builders<PlayerID>.Filter.Eq(player => player.Name, playersNames[i]);
            var update = Builders<PlayerID>.Update.Set(player => player.Score, playersTotalMoneys[i].ToString());

            await collection.UpdateOneAsync(filter, update);
        }
    }
}
