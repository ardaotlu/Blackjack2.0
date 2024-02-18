using MongoDB.Driver;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using MongoDB.Driver;
using MongoDB;
using System.Linq;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    Dictionary<string, int> playerScoreDb = new Dictionary<string, int>();

    Dictionary<string, string> playerPasswordDb = new Dictionary<string, string>();

    string Names = "";
    string Scores = "";
    public Text myText;
    public Text myText2;
    public Text Warning;
    public Text username;
    public Text password;
    public InputField Pass;
    public Text usernameSet;
    public Text scoreSet;


    [SerializeField] private Canvas afterLogin = null;
    [SerializeField] private Canvas beforeLogin = null;

    private void Awake()
    {
    }
    void Start()
    {
        afterLogin.enabled = false;

        GetHighScores();
        Pass.contentType = InputField.ContentType.Password;
    }

    async void GetHighScores()
    {
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
            playerPasswordDb.Add(result.Name, result.Password);
            playerScoreDb.Add(result.Name, int.Parse(result.Score));
        }

        var orderedDict = from entry in playerScoreDb orderby entry.Value descending select entry;

        Dictionary<string, int> orderedPlayerScoreDb = orderedDict.ToDictionary<KeyValuePair<string, int>, string, int>(pair => pair.Key, pair => pair.Value);
        Names = "";
        Scores = "";
        int l = 1;
        foreach (string result in orderedPlayerScoreDb.Keys)
        {
            Names += l.ToString()+". "+result + "\n";
            Scores += orderedPlayerScoreDb[result] + "\n";
            l++;
        }
        myText.text = Names;
        myText2.text = Scores;

    }



   
    public void Register()
    {
        if (username.text == "" || username.text.Length < 3)
            Warning.text = "Username must contain at least 3 characters";
        else if (playerPasswordDb.ContainsKey(username.text))
            Warning.text = "Username already taken";
        else if (Pass.text == "" || Pass.text.Length < 4)
            Warning.text = "Password must contains at least 4 characters";
        else
        {
            PlayerID player = new PlayerID { Name = username.text, Password = Pass.text, Score = "10000" };
            InsertToDB(player);
            Warning.text=$"Dear {username.text}, welcome!\nEnjoy your game!";
        }
    }
    private async void InsertToDB(PlayerID player)
    {
        const string connectionUri = "mongodb+srv://arda:admin@cluster0.qcgp3dk.mongodb.net/?retryWrites=true&w=majority";
        var settings = MongoClientSettings.FromConnectionString(connectionUri);

        // Create a new client and connect to the server
        var client = new MongoClient(settings);
        string databaseName = "gameDB";
        string collectionName = "Player";
        var db = client.GetDatabase(databaseName);
        var collection = db.GetCollection<PlayerID>(collectionName);
        await collection.InsertOneAsync(player);

        var results = await collection.FindAsync(_ => true);
        playerPasswordDb.Clear();
        playerScoreDb.Clear();

        foreach (var result in results.ToList())
        {
            playerPasswordDb.Add(result.Name, result.Password);
            playerScoreDb.Add(result.Name, int.Parse(result.Score));
        }

        var orderedDict = from entry in playerScoreDb orderby entry.Value descending select entry;
        Dictionary<string, int> orderedPlayerScoreDb = orderedDict.ToDictionary<KeyValuePair<string, int>, string, int>(pair => pair.Key, pair => pair.Value);

        Names = "";
        Scores = "";
        int l = 1;
        foreach (string result in orderedPlayerScoreDb.Keys)
        {
            Names += l.ToString() + ". " + result + "\n";
            Scores += orderedPlayerScoreDb[result] + "\n";
            l++;
        }

        myText.text = Names;
        myText2.text = Scores;
    }

    public void OnLoginButtonPressed()
    {
        if (username.text == "")
            Warning.text = "Username can't be empty";
        else if (!playerPasswordDb.ContainsKey(username.text))
            Warning.text = "Username not found";
        else if (playerPasswordDb[username.text] == Pass.text)
        {
            Warning.text = "Login successful";
            usernameSet.text = username.text;
            scoreSet.text = playerScoreDb[username.text].ToString();
            beforeLogin.enabled = false;
            afterLogin.enabled = true;
        }

        else
            Warning.text = "Wrong Password";
    }



}
