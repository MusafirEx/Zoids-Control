using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleContextData
{
    public int areaId = -1;
    public string areaName = "";
    public string battleType = "AreaBattle";
    public GameObject environmentPrefab;
    public bool isNaturalArea = true;

    public int playerFactionId = -1;
    public string playerFactionName = "";
    public int enemyFactionId = -1;
    public string enemyFactionName = "";

    public int playerFactionSlotIndex = 0;
    public int enemyFactionSlotIndex = 1;

    public List<int> playerUnitIds = new List<int>();
    public List<int> enemyUnitIds = new List<int>();

    public bool IsValid()
    {
        return areaId >= 0 &&
               playerUnitIds != null && playerUnitIds.Count > 0 &&
               enemyUnitIds != null && enemyUnitIds.Count > 0;
    }
}
