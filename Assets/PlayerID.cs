using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public class PlayerID
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]

    public string Id { get; set; }

    public string Name { get; set; }
    public string Password { get; set; }
    public string Score { get; set; }
    public string Dc { get; set; }


}