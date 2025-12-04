using System;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public string PlayerID;
    public float PositionX;
    public float PositionY;
    public float PositionZ;

    public PlayerSaveData() { }

    public PlayerSaveData(string playerID, Vector3 pos)
    {
        PlayerID = playerID;
        PositionX = pos.x;
        PositionY = pos.y;
        PositionZ = pos.z;
    }

    public Vector3 GetPosition() => new Vector3(PositionX, PositionY, PositionZ);
}