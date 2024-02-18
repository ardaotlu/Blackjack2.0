using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kart
{
    private static int cardID = 0;

    public int ID = 0;


    public string Type { get; set; }
    public int Mag { get; set; }

    public Kart(string type, int value)
    {
        ID = GetNextID();
        Type = type;
        Mag = value;
    }

    protected int GetNextID()
    {
        return ++cardID;
    }
}
