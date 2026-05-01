using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DummyAreaDefinition
{
    [Header("Area")]
    public int areaId = 0;
    public string areaName = "New Area";
    public bool isNaturalArea = true;
    public string battleType = "AreaBattle";

    [Header("Environment")]
    public GameObject environmentPrefab;

    [Header("Enemy Setup")]
    public int enemyFactionId = 1;
    public string enemyFactionName = "Enemy";
    public List<int> enemyUnitIds = new List<int>();

    public bool IsValid()
    {
        return areaId >= 0 && enemyUnitIds != null && enemyUnitIds.Count > 0;
    }
}
