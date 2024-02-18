using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckGenerator : MonoBehaviour
{
    Hashtable deste = new Hashtable();
    int ID = 0;
    System.Random randomer = new System.Random();
    List<int> myRandom = new List<int>();

    public Hashtable GenerateDeck()
    {
        for (int k = 0; k < 8; k++)
        {
            for (int i = 2; i < 15; i++)
            {
                ID++;
                Kart myKart = new Kart("kupa", i);
                deste.Add(ID, myKart);
            }
            for (int i = 2; i < 15; i++)
            {
                ID++;
                Kart myKart = new Kart("karo", i);
                deste.Add(ID, myKart);
            }
            for (int i = 2; i < 15; i++)
            {
                ID++;
                Kart myKart = new Kart("sinek", i);
                deste.Add(ID, myKart);
            }
            for (int i = 2; i < 15; i++)
            {
                ID++;
                Kart myKart = new Kart("maca", i);
                deste.Add(ID, myKart);
            }
        }
        return deste;
    }

    public List<int> GenerateRandom()
    {
        while (true)
        {
            int i = 0;
            int myTempRandom = randomer.Next(1, 417);
            if (!myRandom.Contains(myTempRandom))
                myRandom.Add(myTempRandom);
            i++;
            if (myRandom.Count == 280)
                break;
        }
        return myRandom;
    }
}
